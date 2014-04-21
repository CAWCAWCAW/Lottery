using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

namespace Lottery
{
    public class LPlayers
    {
        public int Index { get; set; }
        public TSPlayer Player { get { return TShock.Players[Index]; } }
        public int guessedtimes = 0;
        public int contribution = 0;
        public List<int> guesses = new List<int>();


        public LPlayers(int index)
        {
            this.Index = index;
        }
    }
}