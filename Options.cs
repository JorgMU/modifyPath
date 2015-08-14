using System;
using System.Collections.Generic;
using System.Text;

class Options
{
  public const string SPLIT_CHAR = ":";
  public const string OPT_CHAR = "/";

  public static readonly char[] SPLIT_CHAR_A = SPLIT_CHAR.ToCharArray();
  public static readonly char[] OPT_CHAR_A = OPT_CHAR.ToCharArray();

  private Dictionary<AllowedOptions, string> _options;
  private bool _casesensitive;
  
  //enum Helpers
  private Dictionary<string, AllowedOptions> _s2ao;
  private Dictionary<AllowedOptions, string> _ao2s;
  private List<AllowedOptions> _activeOptions;

  private void buildEnumHelpers()
  {
    _s2ao = new Dictionary<string, AllowedOptions>();
    _ao2s = new Dictionary<AllowedOptions, string>();

    foreach (AllowedOptions ao in Enum.GetValues(typeof(AllowedOptions)))
    {
      if (_casesensitive)
      {
        _s2ao.Add(ao.ToString(), ao);
        _ao2s.Add(ao, ao.ToString());
      }
      else
      {
        _s2ao.Add(ao.ToString().ToUpper(), ao);
        _ao2s.Add(ao, ao.ToString().ToUpper());
      }
    }
  }

  public Options(string[] args) : this(args, false) {}

  public Options(string[] args, bool CaseSensitive)
  {
    _options = new Dictionary<AllowedOptions, string>();
    _activeOptions = new List<AllowedOptions>();
    _casesensitive = CaseSensitive;
    buildEnumHelpers();
    _options = parse(args, CaseSensitive);
    _activeOptions = new List<AllowedOptions>(_options.Keys);
  }

  public string this[AllowedOptions Option] 
  {
    get
    {
      if (_options.ContainsKey(Option)) return _options[Option];
      return "";
    }
    set
    {
      if (_options.ContainsKey(Option))
        _options[Option] = value;
    }
  }

  public string this[string Option]
  {
    get
    {
      if (_s2ao.ContainsKey(Option))
      {
        AllowedOptions o = _s2ao[Option];
        if(_activeOptions.Contains(o))
          return _options[_s2ao[Option]];
      }
      return "";
    }
  }

  private Dictionary<AllowedOptions,string> parse(string[] args, bool IsCaseSensitive)
  {
    Dictionary<AllowedOptions, string> result = new Dictionary<AllowedOptions, string>();

    foreach (string s in args)
    {
      string[] parts = s.Split(SPLIT_CHAR_A, 2, StringSplitOptions.RemoveEmptyEntries);

      string key;
      if (IsCaseSensitive) key = parts[0].Trim(OPT_CHAR_A).Trim();
      else key = parts[0].ToUpper().Trim(OPT_CHAR_A).Trim();

      if (_s2ao.ContainsKey(key))
      {
        string value = "";
        if (parts.Length == 2) value = parts[1];
        result.Add(_s2ao[key], value);
      }
      else
      {
        Console.WriteLine("* Ignoring invalid option: " + key);
      }
    }

    return result;

    //end Parse
  }

  public bool OptionExists(AllowedOptions Option) {
    return _options.ContainsKey(Option);
  }

  public AllowedOptions[] ActiveOptions
  {
    get { return _activeOptions.ToArray(); }
  }

  public override string ToString()
  {
    StringBuilder result = new StringBuilder("Raw Options: \r\n");
    foreach(AllowedOptions o in _ao2s.Keys)
      if(_options.ContainsKey(o))
        if(_options[o] != "")
          result.AppendFormat(" - {0}: {1}\r\n", o.ToString(), _options[o]);
        else
          result.AppendFormat(" - {0}\r\n", o.ToString());
    return result.ToString();
  }

  //end class Options
}
