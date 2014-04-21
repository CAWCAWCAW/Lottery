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
        public static LotteryMain LotteryMain;
        private Timer LotteryTimer;

            public LTimer()
            {
                    LotteryTimer = new Timer(60 * 1000);
            }

            public void Start()
            {

                LotteryTimer.Enabled = true;
                LotteryTimer.Elapsed += updateTimer;
            }


            private void updateTimer(object sender, ElapsedEventArgs args)
            {
                if (LotteryMain.LotteryRunning)
                {
                    TSPlayer.All.SendInfoMessage("A lottery is running with a total of {0}. The number is between 0 and 5,000", LotteryMain.LotteryTotal);
                }
            }
        }
}
