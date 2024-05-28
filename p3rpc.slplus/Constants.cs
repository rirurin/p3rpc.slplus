namespace p3rpc.slplus
{
    public static class Constants
    {
        public static readonly string[] YAML_EXTENSION = { "yaml", "yml" };
        public static readonly string CampCommuTextures = "/Game/Xrd777/UI/Camp/Commu/Textures/";
        public static readonly string BMD_SOURCE_EXTENSION = "msg";
        public static readonly string CampCommuBmds = "/Game/Xrd777/Community/Help/";
        public static readonly string RankUpTextures = "/Game/Xrd777/UI/Community/RankUp/";

        public static string MakeAssetPath(string path) => $"{path}.{path.Split("/")[^1]}";
    }
}
