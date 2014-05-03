using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Threading;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Newtonsoft.Json;
using Wolfje.Plugins.SEconomy;

namespace Lottery
{
    public class Configfile
    {
        public static Config config;
        private static void CreateConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "LotteryConfig.json");
            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        config = new Config();
                        var configString = JsonConvert.SerializeObject(config, Formatting.Indented);
                        sr.Write(configString);
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
            }
        }

        public static bool SaveConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "LotteryConfig.json");

            if (File.Exists(filepath))
            {
                File.WriteAllText(filepath, JsonConvert.SerializeObject(config, Formatting.Indented));
                Log.ConsoleInfo("[Lottery] The lottery total has been saved.");
                return true;
            }
            else
            {
                Log.ConsoleError("Lottery config not found. Creating new one...");
                CreateConfig();
                return false;
            }
        }

        public static bool ReadConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "LotteryConfig.json");
            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            config = JsonConvert.DeserializeObject<Config>(configString);
                        }
                        stream.Close();
                    }
                    return true;
                }
                else
                {
                    Log.ConsoleError("Lottery config not found. Creating new one...");
                    CreateConfig();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
            }
            return false;
        }
        public class Config
        {
            public int MinimumLotteryNumber = 0;
            public int MaximumLotteryNumber = 5000;
            public int LotteryReminderTimeMinutes = 1;
            public string RandomHintRanges = "The range below is plus or minus, and is randomly assigned each lottery.";
            public int RandomHintRange = 200;
            public int MinimumCashVoteAmount = 0;
            public string LotteryTotalStartAmount = "0p0g0s0c";
            public bool LotteryCanSave = true;
            public int LotterySaveTimerMinutes = 5;
            //public bool AwardOnPartialCorrect = false;

        }

        private void Reload_Config(CommandArgs args)
        {
            if (ReadConfig())
            {
                args.Player.SendMessage("Lottery config reloaded sucessfully.", Color.Yellow);
            }
            else
            {
                args.Player.SendErrorMessage("Lottery config reloaded unsucessfully. Check logs for details.");
            }
        }
    }
}
