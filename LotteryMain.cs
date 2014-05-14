using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Threading;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Wolfje.Plugins.SEconomy;

namespace Lottery
{
    [ApiVersion(1, 16)]
    public class LotteryMain : TerrariaPlugin
    {
        private static LTimer lottotimer;
        public static LPlayers LPlayer;
        private bool Hint = true;

        public override string Author { get { return "CAWCAWCAW"; } }
        public override string Description { get { return "A simple lottery script."; } }
        public override string Name { get { return "Lottery"; } }
        public override Version Version { get { return new Version("1.2"); } }


        public LotteryMain(Main game)
            : base(game) { }

        public override void Initialize()
        {
            Configfile.ReadConfig();
            TShockAPI.Commands.ChatCommands.Add(new Command("caw.lottery", Lottery, "lottery"));
            TShockAPI.Commands.ChatCommands.Add(new Command("caw.reloadlottery", Reload_Config, "reloadlottery"));
            lottotimer = new LTimer();
            lottotimer.Start();
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            }
            base.Dispose(disposing);
        }

        #region Playerlist OnJoin/OnLeave
        public void OnJoin(JoinEventArgs args)
        {
            Playerlist[args.Who] = new LPlayers(args.Who);
        }
        public void OnLeave(LeaveEventArgs args)
        {
            Playerlist[args.Who] = null;
        }
        #endregion

        #region Lottery Commands

        public LPlayers[] Playerlist = new LPlayers[256];
        public static Money Lotterytotalmoney;
        public int LotteryWinningNumer;
        public Money amount;
        public int numberguessed;
        public int Lotterynumberhigh;
        public int Lotterynumberlow;
        public static bool LotteryRunning = false;
        public static bool LotteryCanSave = true;


        public void Lottery(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                if (args.Player.Group.HasPermission("caw.lotteryadmin"))
                {
                args.Player.SendErrorMessage("Invalid syntax! Use /lottery [start/guess/total/pastguesses/hint/help/save/lotteryreload/cancel]");
                return;
                }
                else
                {
                    args.Player.SendErrorMessage("Invalid syntax! Use /lottery [start/guess/total/pastguesses/hint/help]");
                    return;
                }
            }
                switch (args.Parameters[0])
                {
                    case "start":
                        if (args.Player.Group.HasPermission("caw.startlottery"))
                        {
                            if (!LotteryRunning)
                            {
                                Random random = new Random();
                                LotteryWinningNumer = random.Next(Configfile.config.MinimumLotteryNumber, (Configfile.config.MaximumLotteryNumber +1 ));
                                LotteryRunning = true;
                                Money.TryParse(Configfile.config.LotteryTotalStartAmount, out Lotterytotalmoney);
                                Lotterynumberhigh = (LotteryWinningNumer + random.Next(1, Configfile.config.RandomHintRange + 1));
                                Lotterynumberlow = (LotteryWinningNumer - random.Next(1, Configfile.config.RandomHintRange + 1));
                                TSPlayer.All.SendSuccessMessage("[Lottery] A lottery has been started type /lottery to see how to join!");
                            }
                            else
                            {
                                args.Player.SendErrorMessage("A lottery has already been started, please finish this one first.");
                            }
                        }
                        else
                        {
                            args.Player.SendErrorMessage("You do not have permission to start a lottery.");
                        }
                        break;

                    case "guess":
                        var Journalpayment = Wolfje.Plugins.SEconomy.Journal.BankAccountTransferOptions.AnnounceToSender;
                        var UserSEAccount = SEconomyPlugin.GetEconomyPlayerByBankAccountNameSafe(args.Player.UserAccountName);
                        var playeramount = UserSEAccount.BankAccount.Balance;

                        if (LotteryRunning)
                        {
                            if (args.Parameters.Count < 2)
                            {
                                args.Player.SendErrorMessage("Invalid syntax! Use /lottery guess <numbertoguess> <money>");
                                return;
                            }
                            if (!int.TryParse(args.Parameters[1], out numberguessed))
                            {
                                args.Player.SendErrorMessage("Error: Non-Numerical Amount Detected!");
                                return;
                            }
                            if (numberguessed > Configfile.config.MaximumLotteryNumber || numberguessed < Configfile.config.MinimumLotteryNumber)
                            {
                                args.Player.SendErrorMessage("The number for this lottery is between {0} and {1}", Configfile.config.MinimumLotteryNumber, Configfile.config.MaximumLotteryNumber);
                                return;
                            }
                            foreach (var player in Playerlist)
                            {
                                    if (Playerlist[player.Index].guesses.Contains(numberguessed))
                                    {
                                        args.Player.SendErrorMessage("You have already guessed {0}, please try a different number.", numberguessed);
                                        return;
                                    }
                                    break;
                            }

                            if (!Money.TryParse(args.Parameters[2], out amount))
                            {
                                args.Player.SendErrorMessage("Error: Non-Numerical Amount Detected!");
                                return;
                            }
                            if (amount <= 0)
                            {
                                args.Player.SendErrorMessage("Error: Amount to put in the lottery must be greater then 0! Amount you entered = {1}", amount);
                                return;
                            }
                            if (amount < Configfile.config.MinimumCashVoteAmount)
                            {
                                args.Player.SendErrorMessage("Error: The money you have put into the lottery does not match the minimum value to play. The minimum value is: {0}", Configfile.config.MinimumCashVoteAmount);
                                return;
                            }

                            Money moneyamount2 = amount;
                            Money moneyamount = -amount;

                            if (playeramount < moneyamount2)
                            {
                                args.Player.SendErrorMessage("Payment failed! You do not have enough money to do that!");
                            }
                            if (playeramount > moneyamount2)
                            {
                                args.Player.SendSuccessMessage("You have guessed {0} and added {1} to the lottery.", numberguessed, moneyamount2);
                                SEconomyPlugin.WorldAccount.TransferToAsync(UserSEAccount.BankAccount, moneyamount, Journalpayment, string.Format("{0} has been added to the lottery.", moneyamount2, args.Player.Name), string.Format("Lottery: " + "Adding money into the pool."));
                                Lotterytotalmoney += amount;
                                Playerlist[args.Player.Index].contribution += amount;
                                Playerlist[args.Player.Index].guessedtimes++;
                                Playerlist[args.Player.Index].guesses.Add(numberguessed);
                                

                            }
                            if (numberguessed == LotteryWinningNumer)
                            {
                                TSPlayer.All.SendInfoMessage("[Lottery] {0} has won the lottery of {1}!", args.Player.Name, Lotterytotalmoney);
                                SEconomyPlugin.WorldAccount.TransferToAsync(UserSEAccount.BankAccount, Lotterytotalmoney, Journalpayment, string.Format("{0} has won the lottery!.", args.Player.Name), string.Format("Lottery: " + "Winning"));
                                Lotterytotalmoney = 0;
                                LotteryRunning = false;
                                foreach (var player in Playerlist)
                                {
                                    if (player != null)
                                    {
                                        player.contribution = 0;
                                        player.guessedtimes = 0;
                                        player.guesses.Clear();
                                    }
                                }
                            }
                        }
                        else
                        {
                            args.Player.SendErrorMessage("A lottery is not running type [/lottery start] to start one.");
                        }
                        break;

                    case "pastguesses":
                        if (LotteryRunning)
                        {
                            if (Playerlist[args.Player.Index].guesses.Count > 0)
                            {
                                args.Player.SendErrorMessage("You have guessed " + Playerlist[args.Player.Index].guessedtimes + " times. Your previous guesses are: " + string.Join(", ", Playerlist[args.Player.Index].guesses));
                            }
                            else
                            {
                                args.Player.SendErrorMessage("You have not guessed or contributed the lottery. Type /lottery to join in!");
                            }
                        }
                        else
                        {
                            args.Player.SendErrorMessage("A lottery is not running type [/lottery start] to start one.");
                        }
                        break;

                    case "hint":
                        if (LotteryRunning)
                        {
                            if (Hint)
                            {
                                if (args.Player.Group.HasPermission("caw.lotteryhint"))
                                {
                                    args.Player.SendInfoMessage("The lottery number could be any number in between {0} and {1}.", Lotterynumberlow, Lotterynumberhigh);
                                }
                                else
                                {
                                    args.Player.SendErrorMessage("You do not have pemission to use this command.");
                                }
                            }
                        }
                        else
                        {
                            args.Player.SendErrorMessage("A lottery is not running type [/lottery start] to start one.");
                        }
                        break;

                    case "total":
                        if (LotteryRunning)
                        {
                            args.Player.SendInfoMessage("The current lottry total is {0}.", Lotterytotalmoney);
                            args.Player.SendErrorMessage("The amount you have put into the pot is: " + Playerlist[args.Player.Index].contribution);
                            if (args.Player.Group.HasPermission("caw.winningnumber"))
                            {
                                args.Player.SendInfoMessage("Winning number is " + LotteryWinningNumer);
                            }
                        }
                        else
                        {
                            args.Player.SendErrorMessage("A lottery is not running type [/lottery start] to start one.");
                        }
                        break;

                    case "cancel":
                        if (LotteryRunning)
                        {
                            if (args.Player.Group.HasPermission("caw.cancellottery"))
                            {
                                Configfile.config.LotteryTotalStartAmount = Lotterytotalmoney.ToString();
                                TSPlayer.All.SendInfoMessage("{0} has canceled the current lottery of {1}, and saved it until the lottery restarts.", args.Player.Name, Lotterytotalmoney);
                                Lotterytotalmoney = 0;
                                LotteryRunning = false;
                                foreach (var player in Playerlist)
                                {
                                    if (player != null)
                                    {
                                        player.contribution = 0;
                                        player.guessedtimes = 0;
                                        player.guesses.Clear();
                                    }
                                }
                            }
                            else
                            {
                                args.Player.SendErrorMessage("You do not have permission to use this command.");
                            }
                        }
                        else
                        {
                            args.Player.SendErrorMessage("To cancel a lottery one must be started.");
                        }
                        break;
                    case "save":
                        if (LotteryCanSave)
                        {
                            if (args.Player.Group.HasPermission("caw.savelottery"))
                            {
                                if (LotteryRunning)
                                {
                                    Configfile.config.LotteryTotalStartAmount = Lotterytotalmoney.ToString();
                                    args.Player.SendSuccessMessage("The config file has saved with a total lottery of {0}. Next time the lottery starts that is the starting value.", Configfile.config.LotteryTotalStartAmount);
                                    Configfile.SaveConfig();
                                }
                            }
                            else
                            {
                                args.Player.SendErrorMessage("You do not have permission to use this command.");
                            }
                        }
                        else
                        {
                            args.Player.SendErrorMessage("The owner has turned the save feature off.");
                        }
                        break;
                    case "help":
                        if (LotteryRunning)
                        {
                            args.Player.SendSuccessMessage("Example of guessing: /lottery guess 210 25g5s");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("A lottery is not started, type /lottery start to get one going!");
                            args.Player.SendErrorMessage("Example of guessing: /lottery guess 210 25g5s");
                        }
                        break;
                }
        }
        #endregion

        #region Reload Config
        private void Reload_Config(CommandArgs args)
        {
            if (Configfile.ReadConfig())
            {
                args.Player.SendMessage("Lottery config reloaded sucessfully.", Color.Yellow);
            }
            else
            {
                args.Player.SendErrorMessage("Lottery config reloaded unsucessfully. Check logs for details.");
            }
        }
        #endregion
    }
}