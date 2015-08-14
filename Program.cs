using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

enum AllowedOptions { Help, Add, Remove, List, User, Process, Machine, Verbose, KeepOrphans, KeepDuplicates, DoNotFixCase, WhatIf }

namespace modifyPath
{
  class Program
  {
    private static readonly string _exe = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
    private static Options _opt;
    private static bool _verbose = false;
    private static bool _keepOrphans = false;
    private static bool _keepDupes = false;
    private static bool _fixCase = false;
    private static bool _whatIf = false;

    static void Main(string[] args)
    {
      _opt = new Options(args, false);

      EnvironmentVariableTarget? evt = null;
      AllowedOptions? ao = null;

      foreach(AllowedOptions o in _opt.ActiveOptions)
        switch (o)
        {
          case AllowedOptions.Help:
            ShowUse("", 0);
            break;
          case AllowedOptions.Add:
            if (ao != null) ShowUse("You can only provide one operation!", -1);
            ao = o;
            break;
          case AllowedOptions.Remove:
            if (ao != null) ShowUse("You can only provide one operation!", -1);
            ao = o;
            break;
          case AllowedOptions.List:
            if (ao != null) ShowUse("You can only provide one operation!", -1);
            ao = o;
            break;
          case AllowedOptions.User:
            if (evt != null) ShowUse("You can only provide one target!", -1);
            evt = EnvironmentVariableTarget.User;
            break;
          case AllowedOptions.Process:
            if (evt != null) ShowUse("You can only provide one target!", -1);
            evt = EnvironmentVariableTarget.Process;
            break;
          case AllowedOptions.Machine:
            if (evt != null) ShowUse("You can only provide one target!", -1);
            evt = EnvironmentVariableTarget.Machine;
            break;
          case AllowedOptions.Verbose:
            _verbose = true;
            break;
          case AllowedOptions.KeepOrphans:
            _keepOrphans = true;
            break;
          case AllowedOptions.KeepDuplicates:
            _keepDupes = true;
            break;
          case AllowedOptions.DoNotFixCase:
            _fixCase = false;
            break;
          case AllowedOptions.WhatIf:
            _whatIf = true;
            break;
          default:
            break;
        }

      Verbose(_opt.ToString());

      if (evt == null) evt = EnvironmentVariableTarget.User;
      if (ao == null) ao = AllowedOptions.List;

      Console.WriteLine("{0}/{1}", evt, ao);

      switch (ao)
      {
        case AllowedOptions.Add:
          break;
        case AllowedOptions.Remove:
          break;
        case AllowedOptions.List:
          List((EnvironmentVariableTarget)evt);
          break;
        default:
          break;
      }

      Console.ReadKey();
    }

    private static void List(EnvironmentVariableTarget Target)
    {
      foreach (DirectoryInfo di in GetCurrentPath(Target))
        if (di.Exists) Console.WriteLine(di);
        else Console.WriteLine("[{0}]", di.FullName);
    }

    private static List<DirectoryInfo> GetCurrentPath(EnvironmentVariableTarget Target)
    {
      List<DirectoryInfo> result = new List<DirectoryInfo>();
      List<string> used = new List<string>();
      foreach (string item in Environment.GetEnvironmentVariable("Path", Target).Split(';'))
      {
        DirectoryInfo di = new DirectoryInfo(item.TrimEnd('\\'));
        if (di.Exists)
        {
          if (_fixCase)
          {
            string s = GetCaseFromFileSystem(di);
            if (s != di.FullName)
            {
              Verbose("Case correction: [{0}][{1}]", di.FullName, s);
              di = new DirectoryInfo(s);
            }
          }
        }
        else
        {
          if (_keepOrphans)
            Verbose("Orphan detected and kept: " + di.FullName);
          else
          {
            Verbose("Orphan skipped: " + di.FullName);
            continue;
          }
        }

        if (!used.Contains(di.FullName))
        {
          used.Add(di.FullName);
          result.Add(di);
        }
        else
        {
          if (_keepDupes)
          {
            Verbose("Duplicate kept: " + di.FullName);
            used.Add(di.FullName);
            result.Add(di);
          }
          else Verbose("Duplicate removed: " + di.FullName);
        }
      }

      return result;
    }

    private static void Verbose(string Message, params object[] Items)
    {
      if (_verbose) Console.WriteLine("v " + Message, Items);
    }

    private static string GetCaseFromFileSystem(DirectoryInfo DirInfo)
    {
      DirectoryInfo parent = DirInfo.Parent;
      if (parent == null) return DirInfo.Name;
      return Path.Combine(GetCaseFromFileSystem(parent),
        parent.GetDirectories(DirInfo.Name)[0].Name);
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
