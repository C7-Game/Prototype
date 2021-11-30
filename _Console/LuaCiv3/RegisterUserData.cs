using MoonSharp.Interpreter;
using QueryCiv3;

namespace LuaCiv3
{
    class LuaFunc
    {
        // Enables these type instances to be accssed directly by Lua
        static internal void RegisterQueryCiv3Types() {
            UserData.RegisterType<SavData>();
            UserData.RegisterType<BicData>();
            UserData.RegisterType<Civ3File>();
            UserData.RegisterType<GameSection>();
            UserData.RegisterType<WrldSection>();
            UserData.RegisterType<MapTile>();
            UserData.RegisterType<ContItem>();
            UserData.RegisterType<LeaderItem>();
            UserData.RegisterType<CityItem>();
            
            UserData.RegisterType<BldgSection>();
            UserData.RegisterType<CtznSection>();
            UserData.RegisterType<CultSection>();
            UserData.RegisterType<DiffSection>();
            UserData.RegisterType<ErasSection>();
            UserData.RegisterType<EspnSection>();
            UserData.RegisterType<ExprSection>();
            UserData.RegisterType<GoodSection>();
            UserData.RegisterType<GovtSection>();
            UserData.RegisterType<PrtoSection>();
            UserData.RegisterType<RaceSection>();
            UserData.RegisterType<TechSection>();
            UserData.RegisterType<TfrmSection>();
            UserData.RegisterType<TerrSection>();
            UserData.RegisterType<WsizSection>();
            UserData.RegisterType<FlavSection>();
        }
    }
}