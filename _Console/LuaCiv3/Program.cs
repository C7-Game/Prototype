using System;
using System.IO;
using QueryCiv3;
using MoonSharp.Interpreter;

namespace LuaCiv3
{
    class Program
    {
        static void Main(string[] args)
        {
            Script lua = new Script();
            string runMe = File.ReadAllText("civ3map.lua");

            DynValue _ = lua.DoString(runMe);

            byte[] defaultBicBytes = QueryCiv3.Util.ReadFile(Util.GetCiv3Path() + @"/Conquests/conquests.biq");
    		mapReader = new QueryCiv3.SavData(QueryCiv3.Util.ReadFile(path), defaultBicBytes);
        }
    }
}
