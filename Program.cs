using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

enum AllowedOptions { Help, Add, Clean, List, Remove, User, Process, Machine,
  Confirm, DoNotFixCase, KeepDuplicates, KeepOrphans, Verbose, WhatIf }

namespace modifyPath
{
  class Program
  {
    private const string ENVNAME = "PathTest"; //for testing
    private static readonly string _exe = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
    private static Options _opt;
    private static bool _verbose = false;
    private static bool _keepOrphans = false;
    private static bool _keepDupes = false;
    private static bool _fixCase = true;
    private static bool _whatIf = false;

    private static readonly List<AllowedOptions> OPS = new List<AllowedOptions>()
    {
      AllowedOptions.Add, AllowedOptions.Clean, AllowedOptions.List, AllowedOptions.Remove
    };

    private static readonly List<AllowedOptions> Targets = new List<AllowedOptions>()
    {
      AllowedOptions.Machine, AllowedOptions.Process, AllowedOptions.User
    };

    private static readonly List<AllowedOptions> FLAGS = new List<AllowedOptions>()
    {
      AllowedOptions.Verbose, AllowedOptions.KeepDuplicates, AllowedOptions.KeepOrphans,
      AllowedOptions.DoNotFixCase, AllowedOptions.Confirm, AllowedOptions.WhatIf
    };


    static void Main(string[] args)
    {
      _opt = new Options(args, false);

      EnvironmentVariableTarget? target = null;
      AllowedOptions? operation = null;

      foreach (AllowedOptions o in _opt.ActiveOptions)
      {
        if (OPS.Contains(o))
        {
          if (operation != null) ShowUse("You can only choose one operation!", -1);
          operation = o;
        }
          else if (Targets.Contains(o))
        {
          if (target != null) ShowUse("You can only choose one target!", -1);
          else if (o == AllowedOptions.Machine) target = EnvironmentVariableTarget.Machine;
          else if (o == AllowedOptions.Process) target = EnvironmentVariableTarget.Process;
          else if (o == AllowedOptions.User) target = EnvironmentVariableTarget.User;
        }
        else if (FLAGS.Contains(o))
        {
          //invert defaults if necessary
          if (o == AllowedOptions.DoNotFixCase) _fixCase = false;
          else if (o == AllowedOptions.KeepDuplicates) _keepDupes = true;
          else if (o == AllowedOptions.KeepOrphans) _keepOrphans = true;
          else if (o == AllowedOptions.Verbose) _verbose = true;
          else if (o == AllowedOptions.WhatIf) _whatIf = true;
        }
      }


      if (target == null) target = EnvironmentVariableTarget.User;
      if (operation == null) operation = AllowedOptions.List;

      Verbose("Operation: " + operation);
      Verbose("Target: " + target);
      Verbose(_opt.ToString());

      switch (operation)
      {
        case AllowedOptions.Add:
          break;
        case AllowedOptions.Remove:
          break;
        case AllowedOptions.List:
          List((EnvironmentVariableTarget)target);
          break;
        default:
          break;
      }

      Console.Write("Press a key to exit...");
      Console.ReadKey();
    }

    private static void Clean(EnvironmentVariableTarget Target)
    {

    }

    private static void List(EnvironmentVariableTarget Target)
    {
      foreach (string path in GetCurrentPath(Target))
        if (Directory.Exists(path)) Console.WriteLine(path);
        else Console.WriteLine("[{0}]", path);
    }

    private static List<string> GetCurrentPath(EnvironmentVariableTarget Target)
    {
      List<string> result = new List<string>();
      List<string> used = new List<string>(); //using paralle list to case insensivity

      foreach (string item in Environment.GetEnvironmentVariable(ENVNAME, Target).Split(';'))
      {
        string cp = item.TrimEnd('\\');
        if (cp == "") continue;

        if (Directory.Exists(cp))
        {
          if (_fixCase)
          {
            string vp = GetCaseFromFileSystem(cp);
            if (vp != cp)
            {
              Verbose("Case correction: {0}", cp);
              cp = vp;
            }
          }
        }
        else
        {
          if (_keepOrphans)
            Verbose("Orphan detected and kept: " + cp);
          else
          {
            Verbose("Orphan skipped: " + cp);
            continue;
          }
        }

        if (used.Contains(cp.ToLower()))
        {
          if (_keepDupes)
          {
            Verbose("Duplicate kept: " + cp);
            result.Add(cp);
          }
          else Verbose("Duplicate skipped: " + cp);
        }
        else
        {
          used.Add(cp.ToLower());
          result.Add(cp);
        }

      }

      return result;
    }

    private static void Verbose(string Message, params object[] Items)
    {
      if (_verbose) Console.WriteLine("v " + Message, Items);
    }

    private static string GetCaseFromFileSystem(string DirPath)
    {
      DirectoryInfo current = new DirectoryInfo(DirPath);
      DirectoryInfo parent = current.Parent;
      if (parent == null) return DirPath;

      return Path.Combine(GetCaseFromFileSystem(parent.FullName),
        parent.GetDirectories(current.Name)[0].Name);
    }

    private static void ShowError(SystemException se, int? Exit)
    {
      Console.WriteLine(se.Message);
      if (se.InnerException != null)
        Console.WriteLine(se.InnerException.Message);
      if (Exit != null) Environment.Exit((int)Exit);
    }

    private static void ShowUse(string Message, int? Exit)
    {
      Console.WriteLine(Message);
      if (Exit != null) Environment.Exit((int)Exit);
    }
  }
}
