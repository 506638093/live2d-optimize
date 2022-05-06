/* 
 * ==============================================================================
 * Filename: CubismStartUp
 * Created:  2022 / 5 / 6
 * Author: HuaHua
 * Purpose: Add OPTIMIZE_LIVE2D Macro
 * ==============================================================================
**/

using UnityEditor;

namespace Live2D.Cubism.Editor
{
    [InitializeOnLoad]
    public static class StartUp
    {
        //private static int tickNum = 0;
        static StartUp()
        {
            AppendMacro("OPTIMIZE_LIVE2D");
        }

        private static void AppendMacro(string macro)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!defines.Contains(macro))
            {
                defines += ";" + macro + ";";
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
        }
    }
}