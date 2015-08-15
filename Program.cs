using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

enum AllowedOptions { Help, Append, Prefix, Clean, List, Remove, User, Process, Machine,
  IgnoreCase, KeepDuplicates, KeepOrphans, Verbose, WhatIf }

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
    private static bool _matchCase = true;
    private static bool _whatIf = false;

    private static readonly List<AllowedOptions> OPS = new List<AllowedOptions>()
    {
      AllowedOptions.Help, AllowedOptions.Append, AllowedOptions.Prefix,
      AllowedOptions.Clean, AllowedOptions.List, AllowedOptions.Remove
    };

    private static readonly List<AllowedOptions> Targets = new List<AllowedOptions>()
    {
      AllowedOptions.Machine, AllowedOptions.Process, AllowedOptions.User
    };

    private static readonly List<AllowedOptions> FLAGS = new List<AllowedOptions>()
    {
      AllowedOptions.Verbose, AllowedOptions.KeepDuplicates, AllowedOptions.KeepOrphans,
      AllowedOptions.IgnoreCase, AllowedOptions.WhatIf
    };
    
    static void Main(string[] args)
    {
      _opt = new Options(args, false);

      if(_opt.HasWarnings)
        ShowUse(string.Join("\r\n",_opt.Warnings.ToArray()), -1);

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
          if (o == AllowedOptions.IgnoreCase) _matchCase = false;
          else if (o == AllowedOptions.KeepDuplicates) _keepDupes = true;
          else if (o == AllowedOptions.KeepOrphans) _keepOrphans = true;
          else if (o == AllowedOptions.Verbose) _verbose = true;
          else if (o == AllowedOptions.WhatIf) _whatIf = true;
        }
      }

      if (target == null) target = EnvironmentVariableTarget.User;
      if (operation == null) operation = AllowedOptions.Help;

      Verbose("Operation: " + operation);
      Verbose("Target: " + target);
      Verbose(_opt.ToString());

      switch (operation)
      {
        case AllowedOptions.Help:
          ShowUse("", 0);
          break;
        case AllowedOptions.Append:
          Add((EnvironmentVariableTarget)target, _opt[(AllowedOptions)operation], true);
          break;
        case AllowedOptions.Prefix:
          Add((EnvironmentVariableTarget)target, _opt[(AllowedOptions)operation], false);
          break;
        case AllowedOptions.Remove:
          Remove((EnvironmentVariableTarget)target, _opt[(AllowedOptions)operation]);
          break;
        case AllowedOptions.List:
          List((EnvironmentVariableTarget)target); //needs to be cast because target is nullable
          break;
        case AllowedOptions.Clean:
          Clean((EnvironmentVariableTarget)target); //needs to be cast because target is nullable
          break;
        default:
          break;
      }


      //this is only for testing
      Console.Write("\r\nPress a key to exit...");
      Console.ReadKey();
    }

    private static void Clean(EnvironmentVariableTarget Target)
    {
      List<string> cleaned = GetCurrentPath(Target);

      if (cleaned.Count < 1)
      {
        Console.WriteLine(ENVNAME + " is empty.");
        return;
      }

      UpdatePath(Target, cleaned);
    }

    private static void UpdatePath(EnvironmentVariableTarget Target, List<string> NewPath)
    {
      string result = string.Join(";", NewPath.ToArray());

      string pre = GetPathSafe(Target);
      if (pre == result)
      {
        Console.WriteLine("\r\nCurrent path matches update, nothing to do.");
        return;
      }

      if (_whatIf)
      {
        Console.WriteLine("\r\nUpdated path:\r\n" + result);
        Console.WriteLine("\r\nWhatIf specified, no action taken.");
        return;
      }

      Verbose("Updated path:\r\n" + result);

      try { Environment.SetEnvironmentVariable(ENVNAME, result, Target); }
      catch (SystemException se) { ShowError(se, -1); }

      string test = GetPathSafe(Target);
      if (test == result)
        Console.WriteLine("Path updated.");
      else
        Console.WriteLine("Failed to update path.");
    }

    private static void List(EnvironmentVariableTarget Target)
    {
      List<string> current = GetCurrentPath(Target);

      if(current.Count < 1)
      {
        Console.WriteLine(ENVNAME + " is empty.");
        return;
      }

      foreach (string path in current)
        if (Directory.Exists(path)) Console.WriteLine(path);
        else Console.WriteLine("[{0}]", path);
    }

    private static void Add(EnvironmentVariableTarget Target, string NewItem, bool Append)
    {
      if(NewItem == "")
        ShowUse("You must provide a path when using ADD", -1);

      string work = NewItem;
      DirectoryInfo di = null;

      try { di = new DirectoryInfo(NewItem); }
      catch (SystemException se) { ShowError(se, -1); }

      if (di.Exists)
      {
         if(_matchCase)
        {
          string s = GetCaseFromFileSystem(di.FullName);
          if(s != work)
          {
            Verbose("Corrected case: " + s);
            work = s;
          }
        } 
      }
      else
      {
        if (!_keepOrphans)
        {
          Console.WriteLine("New path item does not exist, you must set KeepOphans to add it.");
          return;
        }
      }

      List<string> original = GetCurrentPath(Target);

      if (Append)
        original.Add(work);
      else
        original.Insert(0, work);

      UpdatePath(Target, original);

    }

    private static void Remove(EnvironmentVariableTarget Target, string Item)
    {
      string toRemove = Item;

      List<string> original = GetCurrentPath(Target);

      if(_matchCase)
      {
        if(!original.Contains(toRemove))
        {
          Console.WriteLine("Could not find item to remove: " + Item);
          return;
        }
      }
      else
      {
        foreach (string p in original)
        {
          if (p.ToLower() == toRemove.ToLower()) toRemove = p;
          Verbose("Corrected case: " + toRemove);
        }

        if (!original.Contains(toRemove))
        {
          Console.WriteLine("Could not find item to remove: " + Item);
          return;
        }
      }

      original.Remove(toRemove);

      UpdatePath(Target, original);

      Console.WriteLine("Item removed: " + toRemove);
    }

    private static string GetPathSafe(EnvironmentVariableTarget Target)
    {
      string result = "";
      try { result = Environment.GetEnvironmentVariable(ENVNAME, Target); }
      catch (SystemException se) { ShowError(se, null); }
      if (result == null) return "";
      return result;
    }

    private static List<string> GetCurrentPath(EnvironmentVariableTarget Target)
    {
      List<string> result = new List<string>();
      List<string> used = new List<string>(); //using paralle list to case insensivity

      string original = GetPathSafe(Target);

      if (original == "") return result;

      if (_whatIf)
        Console.WriteLine("\r\nOriginal path:\r\n" + original);
      else
        Verbose("Original path:\r\n" + original);

      foreach (string item in original.Split(';'))
      {
        string cp = item.TrimEnd('\\');
        if (cp == "") continue;

        if (Directory.Exists(cp))
        {
          if (_matchCase)
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
      Console.WriteLine(@"modifyPath - jorgie@missouri.edu - 2015

  This is a simple utility help you clean and update your path

  Use: modifyPath.exe operation:data target flag(s)

  Operations:

    {0,-6}    - show this info (default)
    {1,-6}:item - add an item at the end of the current path 
    {2,-6}:item - add an item at the begining of the current path 
    {3,-6}      - show the current path
    {4,-6}      - remove dupes, and fix the case of the current path
    {5,-6}:item - remove and item from the current path

  Targets: ({6}), {7}, {8}

  Flags: {9}, {10}, {11}, {12}, {13}


  Example: modifyPath.exe Append:c:\temp Machine WhatIf

  This example would append c:\temp to the end of the current Machine path,
  and show you the result. Because of the WhatIf flag, it would not actually
  update the environment.
",
      "(" + AllowedOptions.Help + ")",
      AllowedOptions.Append,
      AllowedOptions.Prefix,
      AllowedOptions.List,
      AllowedOptions.Clean,
      AllowedOptions.Remove,
      AllowedOptions.User,
      AllowedOptions.Machine,
      AllowedOptions.Process,
      AllowedOptions.IgnoreCase,
      AllowedOptions.KeepDuplicates,
      AllowedOptions.KeepOrphans,
      AllowedOptions.WhatIf,
      AllowedOptions.Verbose
      );

      Console.WriteLine("\r\n" + Message);
      if (Exit != null) Environment.Exit((int)Exit);
    }
  }
}
