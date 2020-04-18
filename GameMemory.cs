using ProcessMemory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;

namespace RE3REmakeSRT
{
    public class GameMemory : IDisposable
    {
        // Private Variables
        private ProcessMemory.ProcessMemory memoryAccess;
        public bool HasScanned { get; private set; }
        public bool ProcessRunning => memoryAccess.ProcessRunning;
        public int ProcessExitCode => memoryAccess.ProcessExitCode;
        private const string IGT_TIMESPAN_STRING_FORMAT = @"hh\:mm\:ss\.fff";

        // Pointers
        private long BaseAddress { get; set; }
        private MultilevelPointer PointerMapID { get; set; }
        private MultilevelPointer PointerDeathCount { get; set; }
        private MultilevelPointer PointerHealCount { get; set; }
        private MultilevelPointer PointerIGT { get; set; }
        private MultilevelPointer PointerRank { get; set; }
        private MultilevelPointer PointerPlayerHP { get; set; }
        private MultilevelPointer PointerSavesCount { get; set; }
        private MultilevelPointer PointerPlayerPoison { get; set; }
        private MultilevelPointer PointerEnemyEntryCount { get; set; }
        private MultilevelPointer[] PointerEnemyEntries { get; set; }
        private MultilevelPointer PointerInventoryCount { get; set; }
        private MultilevelPointer[] PointerInventoryEntries { get; set; }
        private MultilevelPointer PointerStatsInfo { get; set; }
        private MultilevelPointer PointerDifficulty { get; set; }

        private MultilevelPointer PointerBox { get; set; }

        // Public Properties
        public int PlayerCurrentHealth { get; private set; }
        public int PlayerMaxHealth { get; private set; }
        public bool PlayerPoisoned { get; private set; }
        public InventoryEntry[] PlayerInventory { get; private set; }
        public int EnemyTableCount { get; private set; }
        public EnemyHP[] EnemyHealth { get; private set; }
        public long IGTRunningTimer { get; private set; }
        public long IGTCutsceneTimer { get; private set; }
        public long IGTMenuTimer { get; private set; }
        public long IGTPausedTimer { get; private set; }
        public int SavesCount { get; private set; }
        public int HandgunKills { get; private set; }
        public int ShotgunKills { get; private set; }
        public int GLauncherKills { get; private set; }
        public int MAGKills { get; private set; }
        public int ARifleKills { get; private set; }
        public int EnemyKills { get; private set; }
        public int MrCharlies { get; private set; }
        public int LocksPicked { get; private set; }
        public int HatsOff { get; private set; }
        public int Rank { get; private set; }
        public string RankName { get; private set; }
        public int Difficulty { get; private set; }
        public string DifficultyName { get; private set; }
        public int Lore { get; private set; }
        public int Attachments { get; private set; }
        public int Weapons { get; private set; }
        public int MapID { get; private set; }
        public int DeathCount { get; private set; }
        public int HealCount { get; private set; }
        public int BoxInteractions { get; private set; }
        public int InventoryCount { get; private set; }
        public float RankScore { get; private set; }

        private TimeSpan SRank;
        private TimeSpan BRank;

        // Public Properties - Calculated
        public long IGTCalculated => unchecked(IGTRunningTimer - IGTCutsceneTimer - IGTPausedTimer);
        public long IGTCalculatedTicks => unchecked(IGTCalculated * 10L);
        public TimeSpan IGTTimeSpan
        {
            get
            {
                TimeSpan timespanIGT;

                if (IGTCalculatedTicks <= TimeSpan.MaxValue.Ticks)
                    timespanIGT = new TimeSpan(IGTCalculatedTicks);
                else
                    timespanIGT = new TimeSpan();

                return timespanIGT;
            }
        }
        public string IGTFormattedString => IGTTimeSpan.ToString(IGT_TIMESPAN_STRING_FORMAT, CultureInfo.InvariantCulture);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proc"></param>
        public GameMemory(int pid)
        {
            memoryAccess = new ProcessMemory.ProcessMemory(pid);
            BaseAddress = NativeWrappers.GetProcessBaseAddress(pid, ProcessMemory.PInvoke.ListModules.LIST_MODULES_64BIT).ToInt64(); // Bypass .NET's managed solution for getting this and attempt to get this info ourselves via PInvoke since some users are getting 299 PARTIAL COPY when they seemingly shouldn't. This is built as x64 only and RE3 is x64 only to my knowledge.

            // Setup the pointers.
            
            PointerIGT = new MultilevelPointer(memoryAccess, BaseAddress + 0x08DAA3F0, 0x60L); // *
            PointerSavesCount = new MultilevelPointer(memoryAccess, BaseAddress + 0x08D7C628, 0x1D8L, 0x198);
            PointerMapID = new MultilevelPointer(memoryAccess, BaseAddress + 0x05589470);
            PointerDeathCount = new MultilevelPointer(memoryAccess, BaseAddress + 0x08DA66F0);
            PointerHealCount = new MultilevelPointer(memoryAccess, BaseAddress + 0x08D783B0, 0x88L, 0x118L);
            PointerRank = new MultilevelPointer(memoryAccess, BaseAddress + 0x08D78258); // *
            PointerPlayerHP = new MultilevelPointer(memoryAccess, BaseAddress + 0x08D7C5E8, 0x50L, 0x20L); // *
            PointerPlayerPoison = new MultilevelPointer(memoryAccess, BaseAddress + 0x08DCB6C0, 0x50L, 0x20L, 0xF8L);

            PointerDifficulty = new MultilevelPointer(memoryAccess, BaseAddress + 0x08D7B548, 0x20L, 0x50L);
            PointerStatsInfo = new MultilevelPointer(memoryAccess, BaseAddress + 0x08D7B548, 0x178L);
            PointerBox = new MultilevelPointer(memoryAccess, BaseAddress + 0x08DAC510, 0x70L);

            PointerEnemyEntryCount = new MultilevelPointer(memoryAccess, BaseAddress + 0x08D7A5A8, 0x30L); // *
            GenerateEnemyEntries();

            PointerInventoryCount = new MultilevelPointer(memoryAccess, BaseAddress + 0x08D7C5E8, 0x50L);
            PointerInventoryEntries = new MultilevelPointer[20];
            for (long i = 0; i < PointerInventoryEntries.Length; ++i)
                PointerInventoryEntries[i] = new MultilevelPointer(memoryAccess, BaseAddress + 0x08D7C5E8, 0x50L, 0x98L, 0x10L, 0x20L + (i * 0x08L), 0x18L); // *

            // Initialize variables to default values.
            PlayerCurrentHealth = 0;
            PlayerMaxHealth = 0;
            PlayerPoisoned = false;
            InventoryCount = 0;
            PlayerInventory = new InventoryEntry[20];
            EnemyHealth = new EnemyHP[32];
            IGTRunningTimer = 0L;
            IGTCutsceneTimer = 0L;
            IGTMenuTimer = 0L;
            IGTPausedTimer = 0L;
            SavesCount = 0;
            Rank = 0;
            RankScore = 0f;
            HandgunKills = 0;
            ShotgunKills = 0;
            GLauncherKills = 0;
            MAGKills = 0;
            ARifleKills = 0;
            EnemyKills = 0;
            MrCharlies = 0;
            LocksPicked = 0;
            HatsOff = 0;
            Difficulty = 0;
            Lore = 0;
            MapID = 0;
            DeathCount = 0;
            HealCount = 0;
            BoxInteractions = 0;
            Attachments = 0;
            Weapons = 0;
        }

        /// <summary>
        /// Dereferences a 4-byte signed integer via the PointerEnemyEntryCount pointer to detect how large the enemy pointer table is and then create the pointer table entries if required.
        /// </summary>
        private void GenerateEnemyEntries()
        {
            EnemyTableCount = PointerEnemyEntryCount.DerefInt(0x1CL); // Get the size of the enemy pointer table. This seems to double (4, 8, 16, 32, ...) but never decreases, even after a new game is started.
            if (PointerEnemyEntries == null || PointerEnemyEntries.Length != EnemyTableCount) // Enter if the pointer table is null (first run) or the size does not match.
            {
                PointerEnemyEntries = new MultilevelPointer[EnemyTableCount]; // Create a new enemy pointer table array with the detected size.
                for (long i = 0; i < PointerEnemyEntries.Length; ++i) // Loop through and create all of the pointers for the table.
                    PointerEnemyEntries[i] = new MultilevelPointer(memoryAccess, BaseAddress + 0x08D7A5A8, 0x30L, 0x20L + (i * 0x08L), 0x300L);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void UpdatePointers()
        {
            PointerDeathCount.UpdatePointers();
            PointerHealCount.UpdatePointers();
            PointerMapID.UpdatePointers();
            PointerIGT.UpdatePointers();
            PointerPlayerHP.UpdatePointers();
            PointerPlayerPoison.UpdatePointers();
            PointerRank.UpdatePointers();
            PointerSavesCount.UpdatePointers();
            PointerStatsInfo.UpdatePointers();
            PointerDifficulty.UpdatePointers();
            PointerBox.UpdatePointers();
            PointerInventoryCount.UpdatePointers();

            PointerEnemyEntryCount.UpdatePointers();
            GenerateEnemyEntries(); // This has to be here for the next part.
            for (int i = 0; i < PointerEnemyEntries.Length; ++i)
                PointerEnemyEntries[i].UpdatePointers();

            for (int i = 0; i < PointerInventoryEntries.Length; ++i)
                PointerInventoryEntries[i].UpdatePointers();
        }

        /// <summary>
        /// This call refreshes important variables such as IGT.
        /// </summary>
        /// <param name="cToken"></param>
        public void RefreshSlim()
        {
            // IGT
            IGTRunningTimer = PointerIGT.DerefLong(0x18);
            IGTCutsceneTimer = PointerIGT.DerefLong(0x20);
            IGTMenuTimer = PointerIGT.DerefLong(0x28);
            IGTPausedTimer = PointerIGT.DerefLong(0x30);
        }


        /// <summary>
        /// This call refreshes everything. This should be used less often. Inventory rendering can be more expensive and doesn't change as often.
        /// </summary>
        /// <param name="cToken"></param>
        public void Refresh()
        {
            // Perform slim lookups first.
            RefreshSlim();

            // Other lookups that don't need to update as often.
            // Player HP
            InventoryCount = PointerInventoryCount.DerefInt(0x90);
            DeathCount = PointerDeathCount.DerefInt(0xC0);
            HealCount = PointerHealCount.DerefInt(0x10);
            PlayerMaxHealth = PointerPlayerHP.DerefInt(0x54);
            PlayerCurrentHealth = PointerPlayerHP.DerefInt(0x58);
            PlayerPoisoned = PointerPlayerPoison.DerefByte(0x258) == 0x01;
            Rank = PointerRank.DerefInt(0x58);
            RankScore = PointerRank.DerefFloat(0x5C);
            SavesCount = PointerSavesCount.DerefInt(0x24);
            Difficulty = PointerDifficulty.DerefInt(0x78);
            MapID = PointerMapID.DerefInt(0x1D0);
            Lore = PointerStatsInfo.DerefInt(0x438);
            Attachments = PointerStatsInfo.DerefInt(0x43C);
            MrCharlies = PointerStatsInfo.DerefInt(0x440);
            Weapons = PointerStatsInfo.DerefInt(0x444);
            LocksPicked = PointerStatsInfo.DerefInt(0x448);
            HatsOff = PointerStatsInfo.DerefInt(0x44C);
            EnemyKills = PointerStatsInfo.DerefInt(0x4B4);
            HandgunKills = PointerStatsInfo.DerefInt(0x4B8);
            ShotgunKills = PointerStatsInfo.DerefInt(0x4BC);
            GLauncherKills = PointerStatsInfo.DerefInt(0x4C0);
            MAGKills = PointerStatsInfo.DerefInt(0x4C4);
            ARifleKills = PointerStatsInfo.DerefInt(0x4C8);
            BoxInteractions = PointerBox.DerefInt(0x18);

            if (Difficulty == 0)
            {
                DifficultyName = "Assist";
                SRank = new TimeSpan(0, 2, 30, 0);
                BRank = new TimeSpan(0, 4, 0, 0);
            }
            else if (Difficulty == 1)
            {
                DifficultyName = "Standard";
                SRank = new TimeSpan(0, 2, 0, 0);
                BRank = new TimeSpan(0, 4, 0, 0);
            }
            else if (Difficulty == 2)
            {
                DifficultyName = "Hardcore";
                SRank = new TimeSpan(0, 1, 45, 0);
                BRank = new TimeSpan(0, 4, 0, 0);
            }
            else if (Difficulty == 3)
            {
                DifficultyName = "Nightmare";
                SRank = new TimeSpan(0, 2, 0, 0);
                BRank = new TimeSpan(0, 4, 0, 0);
            }
            else if (Difficulty == 4)
            {
                DifficultyName = "Inferno";
                SRank = new TimeSpan(0, 2, 0, 0);
                BRank = new TimeSpan(0, 4, 0, 0);
            }

            if (IGTTimeSpan <= SRank && SavesCount <= 5) { RankName = "S"; }
            else if (IGTTimeSpan <= SRank && SavesCount > 5) { RankName = "A"; }
            else if (IGTTimeSpan > SRank && IGTTimeSpan <= BRank) { RankName = "B"; }
            else if (IGTTimeSpan > BRank) { RankName = "C"; }

            // Enemy HP
            GenerateEnemyEntries();
            for (int i = 0; i < PointerEnemyEntries.Length; ++i)
                EnemyHealth[i] = new EnemyHP(PointerEnemyEntries[i].DerefInt(0x54), PointerEnemyEntries[i].DerefInt(0x58));

            // Inventory
            for (int i = 0; i < PointerInventoryEntries.Length; ++i)
            {
                long invDataPointer = PointerInventoryEntries[i].DerefLong(0x10);
                long invDataOffset = invDataPointer - PointerInventoryEntries[i].Address;
                int invSlot = PointerInventoryEntries[i].DerefInt(0x28);
                byte[] invData = PointerInventoryEntries[i].DerefByteArray(invDataOffset + 0x10, 0x14);
                PlayerInventory[i] = new InventoryEntry(invSlot, (invData != null) ? invData : new byte[20] { 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 });
            }

            HasScanned = true;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (memoryAccess != null)
                        memoryAccess.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~REmake1Memory() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
