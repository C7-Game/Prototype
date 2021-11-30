using System;
using System.IO;
using MoonSharp.Interpreter;
using QueryCiv3;

namespace LuaCiv3
{
    class Program
    {
        static void Main(string[] args)
        {
            Script lua = new Script();
            LuaFunc.RegisterQueryCiv3Types();
            lua.DoString(File.ReadAllText("process.lua"));
            string civ3Path = (string)lua.Globals["civ3_home"];
            byte[] defaultBicBytes = QueryCiv3.Util.ReadFile(civ3Path + @"/Conquests/conquests.biq");
            foreach (string path in args)
            {
                Console.WriteLine(path);
                if (path.EndsWith("SAV", StringComparison.CurrentCultureIgnoreCase))
                {
                    SavData sav = new QueryCiv3.SavData(QueryCiv3.Util.ReadFile(path), defaultBicBytes);
                    DynValue _ = lua.Call(lua.Globals["process_save"], sav);
                }
                else
                {
                    byte[] bicBytes = QueryCiv3.Util.ReadFile(path);
                    BicData bic = new QueryCiv3.BicData(QueryCiv3.Util.ReadFile(path));
                    DynValue _ = lua.Call(lua.Globals["process_bic"], bic);
                }
            }
            DynValue __ = lua.Call(lua.Globals["show_results"]);
        }
    }
}
