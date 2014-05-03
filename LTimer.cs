using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using TShockAPI;
using Terraria;
using Wolfje.Plugins.SEconomy;

namespace Lottery
{
    public class LTimer
    {
        public static LPlayers LPlayers;
        private Timer LotteryTimer;
        private Timer LotterySaveTimer;

            public LTimer()
            {
                    LotteryTimer = new Timer((Configfile.config.LotteryReminderTimeMinutes * 60) * 1000);
                    LotterySaveTimer = new Timer((Configfile.config.LotterySaveTimerMinutes * 60) * 1000);
            }

            public void Start()
            {

                LotteryTimer.Enabled = true;
                LotteryTimer.Elapsed += updateTimer;

                LotterySaveTimer.Enabled = true;
                LotterySaveTimer.Elapsed += SaveTimer;
            }


            private void updateTimer(object sender, ElapsedEventArgs args)
            {
                if (LotteryMain.LotteryRunning)
                {
                    TSPlayer.All.SendInfoMessage("[Lottery] A lottery is running with a total of {0}. The number is between {1} and {2}", LotteryMain.Lotterytotalmoney, Configfile.config.MinimumLotteryNumber, Configfile.config.MaximumLotteryNumber);
                    TSPlayer.All.SendInfoMessage("[Lottery] Type /lottery to figure out how to play!");
                }
            }
            private void SaveTimer(object sender, ElapsedEventArgs args)
            {
                if (Configfile.config.LotteryCanSave)
                {
                    if (LotteryMain.LotteryRunning)
                    {
                        Configfile.config.LotteryTotalStartAmount = LotteryMain.Lotterytotalmoney.ToString();
                        TSPlayer.All.SendSuccessMessage("[Lottery] The current lottery total has been saved in the configuration file.");
                        Configfile.SaveConfig();
                    }
                }
            }
        }
}
