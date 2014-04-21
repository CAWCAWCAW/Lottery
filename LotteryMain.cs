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
    [ApiVersion(1, 15)]
    public class LotteryMain : TerrariaPlugin
    {
        private static LTimer lottotimer;
        private bool Hint = true;

        public override string Author { get { return "CAWCAWCAW"; } }
        public override string Description { get { return "A simple lottery script."; } }
        public override string Name { get { return "Lottery"; } }
        public override Version Version { get { return new Version("1.0"); } }


        public LotteryMain(Main game)
            : base(game) { }

        public override void Initialize()
        {
            TShockAPI.Commands.ChatCommands.Add(new Command("caw.lottery", Lottery, "lottery"));
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
        public int LotteryTotal;
        public int LotteryWinningNumer;
        public int amount;
        public int numberguessed;
        public int Lotterynumberhigh;
        public int Lotterynumberlow;
        public bool LotteryRunning = false;


        public void Lottery(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                if (args.Player.Group.HasPermission("caw.lotteryadmin"))
                {
                args.Player.SendErrorMessage("Invalic syntax! Use /lottery [start/guess/pastguesses/hint/cancel]");
                return;
                }
                else
                {
                    args.Player.SendErrorMessage("Invalic syntax! Use /lottery [start/guess/pastguesses/hint]");
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
                                LotteryWinningNumer = random.Next(0, 5000);
                                LotteryRunning = true;
                                LotteryTotal = 0;
                                Lotterynumberhigh = (LotteryWinningNumer + random.Next(1, 200));
                                Lotterynumberlow = (LotteryWinningNumer - random.Next(1, 200));
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
                                args.Player.SendErrorMessage("Invalid syntax! Use /lottery guess numbertoguess money");
                                return;
                            }
                            if (!int.TryParse(args.Parameters[1], out numberguessed))
                            {
                                args.Player.SendErrorMessage("Error: Non-Numerical Amount Detected!");
                                return;
                            }
                            if (numberguessed > 5000 || numberguessed < 0)
                            {
                                args.Player.SendErrorMessage("The number for this lottery is between 0 and 5,000");
                                return;
                            }
                            if (!int.TryParse(args.Parameters[2], out amount))
                            {
                                args.Player.SendErrorMessage("Error: Non-Numerical Amount Detected!");
                                return;
                            }
                            if (amount <= 0)
                            {
                                args.Player.SendErrorMessage("Error: Amount to put in the lottery must be greater then 0! Amount you entered = {1}", amount);
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
                                args.Player.SendSuccessMessage("You have added {0} to the lottery.", moneyamount2);
                                SEconomyPlugin.WorldAccount.TransferToAsync(UserSEAccount.BankAccount, moneyamount, Journalpayment, string.Format("{0} has been added to the lottery.", moneyamount2, args.Player.Name), string.Format("Lottery: " + "Adding money into the pool."));
                                LotteryTotal += amount;
                                Playerlist[args.Player.Index].contribution += amount;
                                Playerlist[args.Player.Index].guessedtimes++;
                                Playerlist[args.Player.Index].guesses.Add(numberguessed);
                                args.Player.SendSuccessMessage("You have guessed {0}. ", numberguessed);
                            }
                            if (numberguessed == LotteryWinningNumer)
                            {
                                TSPlayer.All.SendInfoMessage("[Lottery] {0} has won the lottery of {1}!", args.Player.Name, LotteryTotal);
                                SEconomyPlugin.WorldAccount.TransferToAsync(UserSEAccount.BankAccount, LotteryTotal, Journalpayment, string.Format("{0} has won the lottery!.", args.Player.Name), string.Format("Lottery: " + "Winning"));
                                LotteryTotal = 0;
                                Playerlist[args.Player.Index].contribution = 0;
                                Playerlist[args.Player.Index].guessedtimes = 0;
                                Playerlist[args.Player.Index].guesses.Clear();
                                LotteryRunning = false;
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
                                args.Player.SendErrorMessage("The amount you have put into the pot is: " + Playerlist[args.Player.Index].contribution);
                            }
                            else
                            {
                                args.Player.SendErrorMessage("You have not guessed or put in a contribution fo the lottery. Type /lottery to join in!");
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
                                if (args.Player.Group.HasPermission("caw.hint"))
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
                            args.Player.SendInfoMessage("The current lottry total is {0}.", LotteryTotal);
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
                                TSPlayer.All.SendInfoMessage(args.Player.Name + " has canceled the current lottery of " + LotteryTotal);
                                LotteryTotal = 0;
                                Playerlist[args.Player.Index].contribution = 0;
                                Playerlist[args.Player.Index].guessedtimes = 0;
                                Playerlist[args.Player.Index].guesses.Clear();
                                LotteryRunning = false;
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
                }
        }
        #endregion
    }
}