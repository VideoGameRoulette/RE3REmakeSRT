using DoubleBuffered;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RE3REmakeSRT
{
    public partial class MainUI : Form
    {
        bool debug = false;
        // How often to perform more expensive operations.
        // 2000 milliseconds for updating pointers.
        // 333 milliseconds for a full scan.
        // 16 milliseconds for a slim scan.
        public const long PTR_UPDATE_TICKS = TimeSpan.TicksPerMillisecond * 2000L;
        public const long FULL_UI_DRAW_TICKS = TimeSpan.TicksPerMillisecond * 333L;
        public const double SLIM_UI_DRAW_MS = 16d;

        private System.Timers.Timer memoryPollingTimer;
        private long lastPtrUpdate;
        private long lastFullUIDraw;

        // Quality settings (high performance).
        private CompositingMode compositingMode = CompositingMode.SourceOver;
        private CompositingQuality compositingQuality = CompositingQuality.HighSpeed;
        private SmoothingMode smoothingMode = SmoothingMode.None;
        private PixelOffsetMode pixelOffsetMode = PixelOffsetMode.Half;
        private InterpolationMode interpolationMode = InterpolationMode.NearestNeighbor;
        private TextRenderingHint textRenderingHint = TextRenderingHint.AntiAliasGridFit;
        private TimeSpan SRank;
        private TimeSpan BRank;

        // Text alignment and formatting.
        private StringFormat invStringFormat = new StringFormat(StringFormat.GenericDefault) { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Far };
        private StringFormat stdStringFormat = new StringFormat(StringFormat.GenericDefault) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };

        private JSONServer jsonServer;
        private Task jsonServerTask;
        private OverlayDrawer overlayDrawer;
        private Task overlayDrawerTask;

        private Bitmap inventoryError; // An error image.
        private Bitmap inventoryItemImage;
        private Bitmap inventoryWeaponImage;

        public enum REDifficultyState : int
        {
            ASSIST,
            STANDARD,
            HARDCORE,
            NIGHTMARE,
            INFERNO
        }

        private string diffName = "";
        private string rankName = "";

        public MainUI()
        {
            InitializeComponent();

            // Set titlebar.
            this.Text += string.Format(" {0}", Program.srtVersion);

            // JSON http endpoint.
            jsonServer = new JSONServer();
            jsonServerTask = jsonServer.Start(CancellationToken.None);

            //GDI+
            this.playerHealthStatus.Paint += this.playerHealthStatus_Paint;
            this.statisticsPanel.Paint += this.statisticsPanel_Paint;
            this.inventoryPanel.Paint += this.inventoryPanel_Paint;

            // DirectX
            overlayDrawer = new OverlayDrawer(Program.gameWindowHandle, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT, Program.programSpecialOptions.ScalingFactor);
            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.DirectXOverlay))
                overlayDrawerTask = overlayDrawer.Run(CancellationToken.None);
            else
                overlayDrawerTask = Task.CompletedTask;

            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoTitleBar))
                this.FormBorderStyle = FormBorderStyle.None;

            if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.Transparent))
                this.TransparencyKey = Color.Black;

            // Only run the following code if we're rendering inventory.
            if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoInventory))
            {
                GenerateImages();

                // Set the width and height of the inventory display so it matches the maximum items and the scaling size of those items.
                this.inventoryPanel.Width = Program.INV_SLOT_WIDTH * 4;
                this.inventoryPanel.Height = Program.INV_SLOT_HEIGHT * 5;

                // Adjust main form width as well.
                this.Width = this.statisticsPanel.Width + 24 + this.inventoryPanel.Width;

                // Only adjust form height if its greater than 461. We don't want it to go below this size.
                if (41 + this.inventoryPanel.Height > 461)
                    this.Height = 41 + this.inventoryPanel.Height;
            }
            else
            {
                // Disable rendering of the inventory panel.
                this.inventoryPanel.Visible = false;

                // Adjust main form width as well.
                this.Width = this.statisticsPanel.Width + 2;
            }

            lastPtrUpdate = DateTime.UtcNow.Ticks;
            lastFullUIDraw = DateTime.UtcNow.Ticks;
        }

        public void GenerateImages()
        {
            // Create a black slot image for when side-pack is not equipped.
            inventoryError = new Bitmap(Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT, PixelFormat.Format32bppPArgb);
            using (Graphics grp = Graphics.FromImage(inventoryError))
            {
                grp.FillRectangle(new SolidBrush(Color.FromArgb(255, 0, 0, 0)), 0, 0, inventoryError.Width, inventoryError.Height);
                grp.DrawLine(new Pen(Color.FromArgb(150, 255, 0, 0), 3), 0, 0, inventoryError.Width, inventoryError.Height);
                grp.DrawLine(new Pen(Color.FromArgb(150, 255, 0, 0), 3), inventoryError.Width, 0, 0, inventoryError.Height);
            }

            // Transform the image into a 32-bit PARGB Bitmap.
            try
            {
                inventoryItemImage = Properties.Resources.ui0100_iam_texout.Clone(new Rectangle(0, 0, Properties.Resources.ui0100_iam_texout.Width, Properties.Resources.ui0100_iam_texout.Height), PixelFormat.Format32bppPArgb);
                inventoryWeaponImage = Properties.Resources.ui0100_wp_iam_texout.Clone(new Rectangle(0, 0, Properties.Resources.ui0100_wp_iam_texout.Width, Properties.Resources.ui0100_wp_iam_texout.Height), PixelFormat.Format32bppPArgb);
            }
            catch (Exception ex)
            {
                Program.FailFast(string.Format("[{0}] An unhandled exception has occurred. Please see below for details.\r\n\r\n[{1}] {2}\r\n{3}.\r\n\r\nPARGB Transform.", Program.srtVersion, ex.GetType().ToString(), ex.Message, ex.StackTrace), ex);
            }

            // Rescales the image down if the scaling factor is not 1.
            if (Program.programSpecialOptions.ScalingFactor != 1d)
            {
                try
                {
                    inventoryItemImage = new Bitmap(inventoryItemImage, (int)Math.Round(inventoryItemImage.Width * Program.programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero), (int)Math.Round(inventoryItemImage.Height * Program.programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero));
                    inventoryWeaponImage = new Bitmap(inventoryWeaponImage, (int)Math.Round(inventoryWeaponImage.Width * Program.programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero), (int)Math.Round(inventoryWeaponImage.Height * Program.programSpecialOptions.ScalingFactor, MidpointRounding.AwayFromZero));
                }
                catch (Exception ex)
                {
                    Program.FailFast(string.Format(@"[{0}] An unhandled exception has occurred. Please see below for details.
---
[{1}] {2}
{3}", Program.srtVersion, ex.GetType().ToString(), ex.Message, ex.StackTrace), ex);
                }
            }
        }

        private void MemoryPollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            bool exitLoop = false;

            try
            {
                bool procRun = Program.gameMemory.ProcessRunning;
                int procExitCode = Program.gameMemory.ProcessExitCode;
                if (!procRun)
                {
                    Program.gamePID = -1;
                    exitLoop = true;
                    return;
                }

                if (Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.AlwaysOnTop))
                {
                    bool hasFocus;
                    if (this.InvokeRequired)
                        hasFocus = PInvoke.HasActiveFocus((IntPtr)this.Invoke(new Func<IntPtr>(() => this.Handle)));
                    else
                        hasFocus = PInvoke.HasActiveFocus(this.Handle);

                    if (!hasFocus)
                    {
                        if (this.InvokeRequired)
                            this.Invoke(new Action(() => this.TopMost = true));
                        else
                            this.TopMost = true;
                    }
                }

                // Only perform a pointer update occasionally.
                if (DateTime.UtcNow.Ticks - lastPtrUpdate >= PTR_UPDATE_TICKS)
                {
                    // Update the last drawn time.
                    lastPtrUpdate = DateTime.UtcNow.Ticks;

                    // Update the pointers.
                    Program.gameMemory.UpdatePointers();
                }

                // Only draw occasionally, not as often as the stats panel.
                if (DateTime.UtcNow.Ticks - lastFullUIDraw >= FULL_UI_DRAW_TICKS)
                {
                    // Update the last drawn time.
                    lastFullUIDraw = DateTime.UtcNow.Ticks;

                    // Get the full amount of updated information from memory.
                    Program.gameMemory.Refresh();

                    // Only draw these periodically to reduce CPU usage.
                    this.playerHealthStatus.Invalidate();
                    if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoInventory))
                        this.inventoryPanel.Invalidate();
                }
                else
                {
                    // Get a slimmed-down amount of updated information from memory.
                    Program.gameMemory.RefreshSlim();
                }

                // Always draw this as these are simple text draws and contains the IGT/frame count.
                this.statisticsPanel.Invalidate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[{0}] {1}\r\n{2}", ex.GetType().ToString(), ex.Message, ex.StackTrace);
            }
            finally
            {
                // Trigger the timer to start once again. if we're not in fatal error.
                if (!exitLoop)
                    ((System.Timers.Timer)sender).Start();
                else
                    CloseForm();
            }
        }

        private void playerHealthStatus_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = smoothingMode;
            e.Graphics.CompositingQuality = compositingQuality;
            e.Graphics.CompositingMode = compositingMode;
            e.Graphics.InterpolationMode = interpolationMode;
            e.Graphics.PixelOffsetMode = pixelOffsetMode;
            e.Graphics.TextRenderingHint = textRenderingHint;

            int x = 20, y = 115;

            // Draw health.
            Font healthFont = new Font("Consolas", 14, FontStyle.Bold);
            
            if (debug)
            {
                //e.Graphics.DrawString(string.Format("MapID: {0}", Program.gameMemory.MapID.ToString()), healthFont, Brushes.White, 0, 0, stdStringFormat);
                e.Graphics.DrawString(string.Format("Width: {0}", this.Width), healthFont, Brushes.White, 0, 0, stdStringFormat);
                e.Graphics.DrawString(string.Format("Height: {0}", this.Height), healthFont, Brushes.White, 0, 15, stdStringFormat);
            }

            if (Program.gameMemory.PlayerPoisoned)
            {
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.POISON, "POISON");
                return;
            }
            else if (Program.gameMemory.PlayerCurrentHealth > 1200 || Program.gameMemory.PlayerCurrentHealth < 0) // Dead?
            {
                e.Graphics.DrawString("DEAD", healthFont, Brushes.Red, x, y, stdStringFormat);
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.EMPTY, "EMPTY");
                return;
            }
            else if (Program.gameMemory.PlayerCurrentHealth >= 801) // Fine (Green)
            {
                e.Graphics.DrawString(Program.gameMemory.PlayerCurrentHealth.ToString(), healthFont, Brushes.LawnGreen, x, y, stdStringFormat);
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.FINE, "FINE");
                return;
            }
            else if (Program.gameMemory.PlayerCurrentHealth <= 800 && Program.gameMemory.PlayerCurrentHealth >= 361) // Caution (Yellow)
            {
                e.Graphics.DrawString(Program.gameMemory.PlayerCurrentHealth.ToString(), healthFont, Brushes.Goldenrod, x, y, stdStringFormat);
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.CAUTION_YELLOW, "CAUTION_YELLOW");
                return;
            }
            else if (Program.gameMemory.PlayerCurrentHealth <= 360) // Danger (Red)
            {
                e.Graphics.DrawString(Program.gameMemory.PlayerCurrentHealth.ToString(), healthFont, Brushes.Red, x, y, stdStringFormat);
                playerHealthStatus.ThreadSafeSetHealthImage(Properties.Resources.DANGER, "DANGER");
                return;
            }
        }

        private void inventoryPanel_Paint(object sender, PaintEventArgs e)
        {
            if (EnableInventory.Checked)
            {
                e.Graphics.SmoothingMode = smoothingMode;
                e.Graphics.CompositingQuality = compositingQuality;
                e.Graphics.CompositingMode = compositingMode;
                e.Graphics.InterpolationMode = interpolationMode;
                e.Graphics.PixelOffsetMode = pixelOffsetMode;
                e.Graphics.TextRenderingHint = textRenderingHint;

                foreach (InventoryEntry inv in Program.gameMemory.PlayerInventory)
                {
                    if (inv == default || inv.SlotPosition < 0 || inv.SlotPosition > 19 || inv.IsEmptySlot)
                        continue;

                    int slotColumn = inv.SlotPosition % 4;
                    int slotRow = inv.SlotPosition / 4;
                    int imageX = slotColumn * Program.INV_SLOT_WIDTH;
                    int imageY = slotRow * Program.INV_SLOT_HEIGHT;
                    int textX = imageX + Program.INV_SLOT_WIDTH;
                    int textY = imageY + Program.INV_SLOT_HEIGHT;
                    bool evenSlotColumn = slotColumn % 2 == 0;
                    Brush textBrush = Brushes.White;

                    if (inv.Quantity == 0)
                        textBrush = Brushes.DarkRed;

                    TextureBrush imageBrush;
                    Weapon weapon;
                    if (inv.IsItem && Program.ItemToImageTranslation.ContainsKey(inv.ItemID))
                    {
                            imageBrush = new TextureBrush(inventoryItemImage, Program.ItemToImageTranslation[inv.ItemID]);
                    }
                    else if (inv.IsWeapon && Program.WeaponToImageTranslation.ContainsKey(weapon = new Weapon() { WeaponID = inv.WeaponID, Attachments = inv.Attachments }))
                        imageBrush = new TextureBrush(inventoryWeaponImage, Program.WeaponToImageTranslation[weapon]);
                    else
                        imageBrush = new TextureBrush(inventoryError, new Rectangle(0, 0, Program.INV_SLOT_WIDTH, Program.INV_SLOT_HEIGHT));

                    // Double-slot item.
                    if (imageBrush.Image.Width == Program.INV_SLOT_WIDTH * 2)
                    {
                        // If we're an odd column, we need to adjust the transform so the image doesn't get split in half and tiled. Not sure why it does this.
                        if (!evenSlotColumn)
                            imageBrush.TranslateTransform(Program.INV_SLOT_WIDTH, 0);

                        // Shift the quantity text over into the 2nd slot's area.
                        textX += Program.INV_SLOT_WIDTH;
                    }

                    e.Graphics.FillRectangle(imageBrush, imageX, imageY, imageBrush.Image.Width, imageBrush.Image.Height);
                    e.Graphics.DrawString((inv.Quantity != -1) ? inv.Quantity.ToString() : "∞", new Font("Consolas", 14, FontStyle.Bold), textBrush, textX, textY, invStringFormat);
                }
            }
        }

        private void statisticsPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = smoothingMode;
            e.Graphics.CompositingQuality = compositingQuality;
            e.Graphics.CompositingMode = compositingMode;
            e.Graphics.InterpolationMode = interpolationMode;
            e.Graphics.PixelOffsetMode = pixelOffsetMode;
            e.Graphics.TextRenderingHint = textRenderingHint;

            // Additional information and stats.
            // Adjustments for displaying text properly.
            int HPBarHeight = 25;
            int xOffset = 10;
            int yOffset = 0;

            //Fonts
            int fontSize = 34;
            Font font = new Font("Consolas", fontSize, FontStyle.Bold);

            int fontSize2 = 20;
            Font font2 = new Font("Consolas", fontSize2, FontStyle.Bold);

            int fontSize3 = 15;
            Font font3 = new Font("Consolas", fontSize3, FontStyle.Bold);

            // IGT Display.
            e.Graphics.DrawString(string.Format("{0}", Program.gameMemory.IGTFormattedString.Remove(8,4)), font, Brushes.White, xOffset, -10, stdStringFormat);
            yOffset += fontSize;

            //Time Spans
            if (Program.gameMemory.Difficulty == (int)REDifficultyState.ASSIST)
            {
                diffName = "Assist";
                SRank = new TimeSpan(0, 2, 30, 0);
                BRank = new TimeSpan(0, 4, 0, 0);
            }
            else if (Program.gameMemory.Difficulty == (int)REDifficultyState.STANDARD)
            {
                diffName = "Standard";
                SRank = new TimeSpan(0, 2, 0, 0);
                BRank = new TimeSpan(0, 4, 0, 0);
            }
            else if (Program.gameMemory.Difficulty == (int)REDifficultyState.HARDCORE)
            {
                diffName = "Hardcore";
                SRank = new TimeSpan(0, 1, 45, 0);
                BRank = new TimeSpan(0, 4, 0, 0);
            }
            else if (Program.gameMemory.Difficulty == (int)REDifficultyState.NIGHTMARE)
            {
                diffName = "Nightmare";
                SRank = new TimeSpan(0, 2, 0, 0);
                BRank = new TimeSpan(0, 4, 0, 0);
            }
            else if (Program.gameMemory.Difficulty == (int)REDifficultyState.INFERNO)
            {
                diffName = "Inferno";
                SRank = new TimeSpan(0, 2, 0, 0);
                BRank = new TimeSpan(0, 4, 0, 0);
            }

            //Ranks
            if (Program.gameMemory.IGTTimeSpan <= SRank && Program.gameMemory.SavesCount <=5) { rankName = "Rank:S"; }
            else if (Program.gameMemory.IGTTimeSpan <= SRank && Program.gameMemory.SavesCount > 5) { rankName = "Rank:A"; }
            else if (Program.gameMemory.IGTTimeSpan > SRank && Program.gameMemory.IGTTimeSpan <= BRank) { rankName = "Rank:B"; }
            else if (Program.gameMemory.IGTTimeSpan > BRank) { rankName = "Rank:C"; }

            if (EnableRankDifficulty.Checked)
            {
                e.Graphics.DrawString(string.Format("{0} {1}", rankName, diffName), font2, Brushes.Gray, xOffset, yOffset, stdStringFormat);
                yOffset += fontSize2;
            }

            if (EnableDARankPoints.Checked)
            {
                yOffset += 10;
                e.Graphics.DrawString(string.Format("DA Rank: {0} DA Score: {1}", Program.gameMemory.Rank, Program.gameMemory.RankScore), font3, Brushes.Gray, xOffset + 1, yOffset, stdStringFormat);
                yOffset += fontSize3;
            }

            if (EnableDeathCounter.Checked)
            {
                yOffset += 10;
                e.Graphics.DrawString(string.Format("Deaths: {0}", Program.gameMemory.DeathCount.ToString()), font3, Brushes.Gray, xOffset + 1, yOffset, stdStringFormat);
                yOffset += fontSize3;
            }
            

            //e.Graphics.DrawString(string.Format("SaveCount:{0}", Program.gameMemory.SavesCount), font, Brushes.Gray, x2, heightOffset + fontSize, stdStringFormat);

            int width = RE3REmakeSRT.Properties.Resources.EMPTY.Width;//300;

            //e.Graphics.DrawString(string.Format("Enemy Count: {0}", Program.gameMemory.EnemyTableCount), font, Brushes.Gray, 0, heightOffset + (heightGap * ++i), stdStringFormat);

            foreach (EnemyHP enemyHP in Program.gameMemory.EnemyHealth.Where(a => a.IsAlive).OrderByDescending(a => a.MaximumHP).ThenByDescending(a => a.Percentage))
            {
                string name = "";
                bool nemesis = false;

                //ASSIST MODE HP VALUES
                if (Program.gameMemory.Difficulty == (int)REDifficultyState.ASSIST)
                {
                    if (enemyHP.MaximumHP == 8000 || enemyHP.MaximumHP == 20000) { nemesis = true; name = "Nemesis"; }
                    else if (enemyHP.MaximumHP == 1500) { name = "Brad"; }
                    else { name = "Enemy"; }
                }
                //STANDARD MODE HP VALUES
                else if (Program.gameMemory.Difficulty == (int)REDifficultyState.STANDARD)
                {
                    if (enemyHP.MaximumHP == 8000 || enemyHP.MaximumHP == 20000) { nemesis = true; name = "Nemesis"; }
                    else if (enemyHP.MaximumHP == 3200) { name = "Hunter γ"; }
                    else if (enemyHP.MaximumHP == 2100) { name = "Hunter β"; }
                    else if (enemyHP.MaximumHP >= 1700 && enemyHP.MaximumHP <= 1800) { name = "Licker"; }
                    else if (enemyHP.MaximumHP == 1500) { name = "Parasite"; }
                    else if (enemyHP.MaximumHP <= 1000) { name = "Infected"; }
                }
                //HARDCORE MODE HP VALUES
                else if (Program.gameMemory.Difficulty == (int)REDifficultyState.HARDCORE)
                {
                    if (enemyHP.MaximumHP == 8000 || enemyHP.MaximumHP == 20000) { nemesis = true; name = "Nemesis"; }
                    else if (enemyHP.MaximumHP == 1500) { name = "Brad"; }
                    else { name = "Enemy"; }
                }
                //NIGHTMARE MODE HP VALUES
                else if (Program.gameMemory.Difficulty == (int)REDifficultyState.NIGHTMARE)
                {
                    if (enemyHP.MaximumHP == 8000 || enemyHP.MaximumHP == 20000) { nemesis = true; name = "Nemesis"; }
                    else if (enemyHP.MaximumHP == 1500) { name = "Brad"; }
                    else { name = "Enemy"; }
                }
                //INFERNO MODE HP VALUES
                else if (Program.gameMemory.Difficulty == (int)REDifficultyState.INFERNO)
                {
                    if (enemyHP.MaximumHP == 8000 || enemyHP.MaximumHP == 20000) { nemesis = true; name = "Nemesis"; }
                    else if (enemyHP.MaximumHP == 1500) { name = "Brad"; }
                    else { name = "Enemy"; }
                }

                //yOffset += HPBarHeight + 10;
                if (enemyHP.MaximumHP > 9999)
                {
                    name = name.PadRight(14, ' ');
                }
                else if (enemyHP.MaximumHP > 999 && enemyHP.MaximumHP < 10000)
                {
                    name = name.PadRight(15, ' ');
                }
                else
                {
                    name = name.PadRight(16, ' ');
                }
                
                string info = "{0} {1} {2:P1}";

                if (EnableEnemy.Checked)
                {
                    yOffset += 8;
                    DrawProgressBarGDI(e, backBrushGDI, foreBrushGDI, xOffset, yOffset, width, HPBarHeight, enemyHP.Percentage * 100f, 100f);
                    e.Graphics.DrawString(string.Format(info, name, enemyHP.CurrentHP, enemyHP.Percentage), font3, Brushes.Red, xOffset, yOffset, stdStringFormat);
                    yOffset += HPBarHeight;
                }
                else
                {
                    
                    if (nemesis)
                    {
                        yOffset += 8;
                        DrawProgressBarGDI(e, backBrushGDI, foreBrushGDI, xOffset, yOffset, width, HPBarHeight, enemyHP.Percentage * 100f, 100f);
                        e.Graphics.DrawString(string.Format(info, name, enemyHP.CurrentHP, enemyHP.Percentage), font3, Brushes.Red, xOffset, yOffset, stdStringFormat);
                        yOffset += HPBarHeight;
                    }
                    
                }
                
            }

            Brush colorBrush;
            string s = "";

            xOffset = 11;
            yOffset += 10;

            if (EnableKills.Checked)
            {
                s = (Program.gameMemory.EnemyKills >= 2000) ? "Enemies Killed:{0}" : "Enemies Killed:{0}/2000";
                colorBrush = SetBrushColor(Program.gameMemory.EnemyKills, 2000);
                e.Graphics.DrawString(string.Format(s, Program.gameMemory.EnemyKills.ToString()), font3, colorBrush, xOffset, yOffset, stdStringFormat);
                yOffset += (int)font3.Size + 4;
                s = (Program.gameMemory.HandgunKills >= 200) ? "Handgun Kills:{0}" : "Handgun Kills:{0}/200";
                colorBrush = SetBrushColor(Program.gameMemory.HandgunKills, 200);
                e.Graphics.DrawString(string.Format(s, Program.gameMemory.HandgunKills.ToString()), font3, colorBrush, xOffset, yOffset, stdStringFormat);
                yOffset += (int)font3.Size + 4;
                s = (Program.gameMemory.ShotgunKills >= 130) ? "Shotgun Kills:{0}" : "Shotgun Kills:{0}/130";
                colorBrush = SetBrushColor(Program.gameMemory.ShotgunKills, 130);
                e.Graphics.DrawString(string.Format(s, Program.gameMemory.ShotgunKills.ToString()), font3, colorBrush, xOffset, yOffset, stdStringFormat);
                yOffset += (int)font3.Size + 4;
                s = (Program.gameMemory.GLauncherKills >= 120) ? "Grenade Launcher Kills:{0}" : "Grenade Launcher Kills:{0}/120";
                colorBrush = SetBrushColor(Program.gameMemory.GLauncherKills, 120);
                e.Graphics.DrawString(string.Format(s, Program.gameMemory.GLauncherKills.ToString()), font3, colorBrush, xOffset, yOffset, stdStringFormat);
                yOffset += (int)font3.Size + 4;
                s = (Program.gameMemory.MAGKills >= 80) ? "MAG Kills:{0}" : "MAG Kills:{0}/80";
                colorBrush = SetBrushColor(Program.gameMemory.MAGKills, 80);
                e.Graphics.DrawString(string.Format(s, Program.gameMemory.MAGKills.ToString()), font3, colorBrush, xOffset, yOffset, stdStringFormat);
                yOffset += (int)font3.Size + 4;
                s = (Program.gameMemory.ARifleKills >= 400) ? "Assault Rifle Kills:{0}" : "Assault Rifle Kills:{0}/400";
                colorBrush = SetBrushColor(Program.gameMemory.ARifleKills, 400);
                e.Graphics.DrawString(string.Format(s, Program.gameMemory.ARifleKills.ToString()), font3, colorBrush, xOffset, yOffset, stdStringFormat);
                yOffset += (int)font3.Size + 4;
            }

            if (EnableCollectables.Checked)
            {
                s = (Program.gameMemory.Lore >= 56) ? "Lore Found:{0}" : "Lore Found:{0}/56";
                colorBrush = SetBrushColor(Program.gameMemory.Lore, 56);
                e.Graphics.DrawString(string.Format(s, Program.gameMemory.Lore.ToString()), font3, colorBrush, xOffset, yOffset, stdStringFormat);
                yOffset += (int)font3.Size + 4;
                s = (Program.gameMemory.Attachments >= 10) ? "Attachments Found:{0}" : "Attachments Found:{0}/10";
                colorBrush = SetBrushColor(Program.gameMemory.Attachments, 10);
                e.Graphics.DrawString(string.Format(s, Program.gameMemory.Attachments.ToString()), font3, colorBrush, xOffset, yOffset, stdStringFormat);
                yOffset += (int)font3.Size + 4;
                s = (Program.gameMemory.MrCharlies >= 20) ? "Mr.Charlies Found:{0}" : "Mr.Charlies Found:{0}/20";
                colorBrush = SetBrushColor(Program.gameMemory.MrCharlies, 20);
                e.Graphics.DrawString(string.Format(s, Program.gameMemory.MrCharlies.ToString()), font3, colorBrush, xOffset, yOffset, stdStringFormat);
                yOffset += (int)font3.Size + 4;
                s = (Program.gameMemory.LocksPicked >= 20) ? "Locks Picked:{0}" : "Locks Picked:{0}/20";
                colorBrush = SetBrushColor(Program.gameMemory.LocksPicked, 20);
                e.Graphics.DrawString(string.Format(s, Program.gameMemory.LocksPicked.ToString()), font3, colorBrush, xOffset, yOffset, stdStringFormat);
            }

        }

        public Brush SetBrushColor(int stat, int max)
        {
            if (stat >= max) { return Brushes.Green; }
            else { return Brushes.Red; }
        }
        // Customisation in future?
        private Brush backBrushGDI = new SolidBrush(Color.FromArgb(255, 60, 60, 60));
        private Brush foreBrushGDI = new SolidBrush(Color.FromArgb(255, 100, 0, 0));

        private void DrawProgressBarGDI(PaintEventArgs e, Brush bgBrush, Brush foreBrush, float x, float y, float width, float height, float value, float maximum = 100)
        {
            // Draw BG.
            e.Graphics.DrawRectangles(new Pen(bgBrush, 2f), new RectangleF[1] { new RectangleF(x, y, width, height) });

            // Draw FG.
            RectangleF foreRect = new RectangleF(
                x + 1f,
                y + 1f,
                (width * value / maximum) - 2f,
                height - 2f
                );
            e.Graphics.FillRectangle(foreBrush, foreRect);
        }

        private void inventoryPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (!Program.programSpecialOptions.Flags.HasFlag(ProgramFlags.NoInventory))
                if (e.Button == MouseButtons.Left)
                    PInvoke.DragControl(((DoubleBufferedPanel)sender).Parent.Handle);
        }

        private void statisticsPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PInvoke.DragControl(((DoubleBufferedPanel)sender).Parent.Handle);
        }

        private void playerHealthStatus_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PInvoke.DragControl(((PictureBox)sender).Parent.Handle);
        }

        private void MainUI_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                PInvoke.DragControl(((Form)sender).Handle);
        }

        private void MainUI_Load(object sender, EventArgs e)
        {
            memoryPollingTimer = new System.Timers.Timer() { AutoReset = false, Interval = SLIM_UI_DRAW_MS };
            memoryPollingTimer.Elapsed += MemoryPollingTimer_Elapsed;
            memoryPollingTimer.Start();
        }

        private void MainUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            memoryPollingTimer.Stop();
            memoryPollingTimer.Dispose();
        }

        private void CloseForm()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() =>
                {
                    this.Close();
                }));
            }
            else
                this.Close();
        }

        private void EnableEnemy_Click(object sender, EventArgs e)
        {
            EnableEnemy.Checked = !EnableEnemy.Checked;
            if (EnableEnemy.Checked)
            {
                this.Height += 305; //1080
                EnableStats.Checked = false;
                EnableKills.Checked = false;
                EnableCollectables.Checked = false;
            }
            else
            {
                this.Height -= 305; //775
            }
        }

        private void EnableStats_Click(object sender, EventArgs e)
        {
            EnableStats.Checked = !EnableStats.Checked;
            if (EnableEnemy.Checked)
            {
                this.Height -= 173;
                EnableEnemy.Checked = false;
                EnableKills.Checked = true;
                EnableCollectables.Checked = true;
            }
            else if (EnableStats.Checked && !EnableEnemy.Checked)
            { 
                this.Height += 198;
                EnableEnemy.Checked = false;
                EnableKills.Checked = true;
                EnableCollectables.Checked = true;
            }
            else
            {
                if (EnableKills.Checked && EnableCollectables.Checked)
                {
                    this.Height -= 205;
                }
                else if (EnableKills.Checked && !EnableCollectables.Checked)
                {
                    this.Height -= 125;
                }
                else if (!EnableKills.Checked && EnableCollectables.Checked)
                {
                    this.Height -= 80;
                }
                else
                {
                    return;
                }
                EnableKills.Checked = false;
                EnableCollectables.Checked = false;
            }
        }

        private void EnableKills_Click(object sender, EventArgs e)
        {
            EnableEnemy.Checked = false;
            EnableKills.Checked = !EnableKills.Checked;
            if (EnableKills.Checked)
            {
                this.Height += 125;
            }
            else
            {
                this.Height -= 125;
            }
        }

        private void EnableCollectables_Click(object sender, EventArgs e)
        {
            EnableEnemy.Checked = false;
            EnableCollectables.Checked = !EnableCollectables.Checked;
            if (EnableCollectables.Checked)
            {
                EnableStats.Checked = true;
                this.Height += 80;
            }
            else
            {
                this.Height -= 80;
            }
        }

        private void EnableInventory_Click(object sender, EventArgs e)
        {
            EnableInventory.Checked = !EnableInventory.Checked;
        }

        private void EnableRankDifficulty_Click(object sender, EventArgs e)
        {
            EnableRankDifficulty.Checked = !EnableRankDifficulty.Checked;
            if (EnableRankDifficulty.Checked)
            {
                this.Height += 20;
            }
            else
            {
                this.Height -= 20;
            }
        }

        private void EnableDARankPoints_Click(object sender, EventArgs e)
        {
            EnableDARankPoints.Checked = !EnableDARankPoints.Checked;
            if (EnableDARankPoints.Checked)
            {
                this.Height += 25;
            }
            else
            {
                this.Height -= 25;
            }
        }

        private void EnableDeathCounter_Click(object sender, EventArgs e)
        {
            EnableDeathCounter.Checked = !EnableDeathCounter.Checked;
            if (EnableDeathCounter.Checked)
            {
                this.Height += 15;
            }
            else
            {
                this.Height -= 15;
            }
        }
    }
}
