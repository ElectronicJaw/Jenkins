using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds project for specified platform into specified output folder.
/// As parameters uses command-line parameters.
/// For target platform uses parameter -target and name of the platform from enum BuildTarget.
/// For output path uses parameter -output and path.
/// All parameters except the path aren't case sensitive.
/// Example of command line: -output some/output/dir/or/file -target android 
/// </summary>
public static class Builder
{
  private const string TargetFlag = "-target";
  private const string OutputFlag = "-output";
  private const string DebugFlag = "-debug";
  private const string AppendFlag = "-append";
  
  private static void Build()
  {
    var commandLineArgs = System.Environment.GetCommandLineArgs();
    BuildTarget target = BuildTarget.Android;
    string path = ParseOutputPath(commandLineArgs);
  
    if (ParseTarget(commandLineArgs, ref target) && !string.IsNullOrEmpty(path))
      Build(target, path, ParseDebug(commandLineArgs), ParseAppend(commandLineArgs));
    else
      throw new ArgumentException(string.Format("Invalid command line arguments:\n{0}", commandLineArgs));
  }
  
  #region Release
  
  [MenuItem("Builder/Release/Build")]
  private static void TestBuildRelease()
  {
    TestBuild(EditorUserBuildSettings.activeBuildTarget, false);
  }
  
  [MenuItem("Builder/Release/Build iOS")]
  private static void TestBuildIosRelease()
  {
    TestBuild(BuildTarget.iPhone, false);
  }

  [MenuItem("Builder/Release/Build Android")]
  private static void TestBuildAndroidRelease()
  {
    TestBuild(BuildTarget.Android, false);
  }
  
  #endregion
  
  #region Debug
  
  [MenuItem("Builder/Debug/Build")]
  private static void TestBuildDebug()
  {
    TestBuild(EditorUserBuildSettings.activeBuildTarget, true);
  }
  
  [MenuItem("Builder/Release/Build PC")]
  private static void TestBuildPCRelease()
  {
    TestBuild(BuildTarget.StandaloneWindows, false);
  }
  
  [MenuItem("Builder/Debug/Build iOS")]
  private static void TestBuildIosDebug()
  {
    TestBuild(BuildTarget.iPhone, true);
  }
  
  [MenuItem("Builder/Debug/Build Android")]
  private static void TestBuildAndroidDebug()
  {
    TestBuild(BuildTarget.Android, true);
  }
  
  #endregion
    
  #region Auxiliary functions
  
  private static void TestBuild(BuildTarget target, bool debug)
  {
    Build(target, EditorUserBuildSettings.GetBuildLocation(target), debug, true);
  }
  
  private static bool ParseTarget(string[] parameters, ref BuildTarget target)
  {
    for (int i = 0; i < parameters.Length; ++i)
    {
      var param = parameters[i];
      if (string.Compare(param, TargetFlag, true) == 0)
      {
        if (i < parameters.Length - 1)
        {
          param = parameters[i + 1];
          // NOTE: we didn't catch the exception because
          // we want it to stop build process.
          target = (BuildTarget)Enum.Parse(typeof(BuildTarget), param, true);
          return true;
        }
      }
    }
    return false;
  }
  
  private static string ParseOutputPath(string[] parameters)
  {
    for (int i = 0; i < parameters.Length; ++i)
    {
      var param = parameters[i];
      if (string.Compare(param, OutputFlag, true) == 0 && i < parameters.Length - 1)
        return parameters[i + 1];
    }
    return string.Empty;
  }

  /// <summary>
  /// Parses command line to check debug flag for debug mode.
  /// </summary>
  /// <returns>
  /// Returns true if debug flag found.
  /// </returns>
  private static bool ParseDebug(string[] parameters)
  {
    for (int i = 0; i < parameters.Length; ++i)
    {
      var param = parameters[i];
      if (string.Compare(param, DebugFlag, true) == 0)
        return true;
    }
    return false;
  }
  
  /// <summary>
  /// Parses command line to check append flag to append xCode project folder, not overwriting it.
  /// </summary>
  /// <returns>
  /// Returns true if append flag found.
  /// </returns>
  private static bool ParseAppend(string[] parameters)
  {
    for (int i = 0; i < parameters.Length; ++i)
    {
      var param = parameters[i];
      if (string.Compare(param, AppendFlag, true) == 0)
        return true;
    }
    return false;
  }
  
  /// <summary>
  /// Build project for specified target platform in outputPath.
  /// </summary>
  /// <param name='target'>
  /// Target platform
  /// </param>
  /// <param name='outputPath'>
  /// Output path for build file or build directory.
  /// </param>
  /// <param name='debug'>
  /// Enables development mode.
  /// </param>
  /// <param name='append'>
  /// For iOs only: doesn't overwrite xCode project folder.
  /// </param>
  private static void Build(BuildTarget target, string outputPath, bool debug = false, bool append = false)
  {
    var paths = from scene in EditorBuildSettings.scenes
                 where scene.enabled && File.Exists(scene.path)
                 select scene.path;
    string[] scenes = paths.ToArray();
    BuildOptions options = BuildOptions.None;
    if (target == BuildTarget.iPhone)
    {
      options |= BuildOptions.SymlinkLibraries;
      if (append)
        options |= BuildOptions.AcceptExternalModificationsToPlayer;
    }
    var strippingLevel = PlayerSettings.strippingLevel;
    if (debug)
    {
      options |= BuildOptions.Development;
      PlayerSettings.strippingLevel = StrippingLevel.Disabled;
    }
      
    Debug.Log(string.Format("Building project for platform {0} into '{1}'", target, outputPath));
    string error = BuildPipeline.BuildPlayer(scenes, outputPath, target, options);
    PlayerSettings.strippingLevel = strippingLevel;
    // throwing exception automatically makes exit code 1 in batch mode.
    if (!string.IsNullOrEmpty(error))
      throw new Exception(error);
  }
  
  #endregion
}