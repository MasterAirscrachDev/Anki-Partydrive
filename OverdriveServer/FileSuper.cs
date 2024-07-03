///FileSuperSystem V2.2

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
/// <summary>
/// This is the Save class, it contains all the data that will be saved
/// </summary>
public class Save
{
    public List<int> Ints = new List<int>();
    public List<bool> Bools = new List<bool>();
    public List<float> Floats = new List<float>();
    public List<string> Strings = new List<string>(), StringNames = new List<string>(), IntNames = new List<string>(), BoolNames = new List<string>(), FloatNames = new List<string>();
    ///<summary>Set an int using the name</summary>
    public void SetInt(string name, int value) {
        if (IntNames.Contains(name)) Ints[IntNames.IndexOf(name)] = value;
        else { IntNames.Add(name); Ints.Add(value); }
    }
    ///<summary>Set a bool using the name</summary>
    public void SetBool(string name, bool value) {
        if (BoolNames.Contains(name)) Bools[BoolNames.IndexOf(name)] = value;
        else { BoolNames.Add(name); Bools.Add(value); }
    }
    ///<summary>Set a float using the name</summary>
    public void SetFloat(string name, float value) {
        if (FloatNames.Contains(name)) Floats[FloatNames.IndexOf(name)] = value;
        else { FloatNames.Add(name); Floats.Add(value); }
    }
    ///<summary>Set a string using the name</summary>
    public void SetString(string name, string value) {
        if (StringNames.Contains(name)) Strings[StringNames.IndexOf(name)] = value;
        else { StringNames.Add(name); Strings.Add(value); }
    }
    ///<summary>Get an int using the name, if its not found return null</summary>
    public int? GetInt(string name) {
        if (IntNames.Contains(name)) return Ints[IntNames.IndexOf(name)];
        else return null;
    }
    ///<summary>Get a bool using the name, if its not found return null</summary>
    public bool? GetBool(string name) {
        if (BoolNames.Contains(name)) return Bools[BoolNames.IndexOf(name)];
        else return null;
    }
    ///<summary>Get a float using the name, if its not found return null</summary>
    public float? GetFloat(string name) {
        if (FloatNames.Contains(name)) return Floats[FloatNames.IndexOf(name)];
        else return null;
    }
    ///<summary>Get a string using the name, if its not found return null</summary>
    public string GetString(string name) {
        if (StringNames.Contains(name)) return Strings[StringNames.IndexOf(name)];
        else return null;
    }
}
/// <summary>
/// This is the FileSuper class, it contains all the functions needed to save and load files
/// </summary>
public class FileSuper
{
    public bool working, debug;
    string encryptKey, fullpath;
    string[] splitSettings;
    ///<summary>Create a new FileSuper object </summary>
    public FileSuper(string project, string studio, bool debug = false, string[] splitSettings = null)
    {
        this.debug = debug;
        this.encryptKey = null;
        if(splitSettings != null){ this.splitSettings = splitSettings;}
        else{ this.splitSettings = new string[] { "\n", "\r\n" };}
        fullpath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\{studio}\\{project}\\";
    }
    ///<summary>Set the encryption key, null = no encryption</summary>
    public void SetEncryption(string key = null) { encryptKey = key; }
    ///<summary>Encrypt or decrypt a string using a key</summary>
    public string EncDecProcess(string dataIn, string key)
    {
        string xorstring = "", input = dataIn, enckey = key;
        for (int i = 0; i < input.Length; i++)
        { xorstring += (char)(input[i] ^ enckey[i % enckey.Length]); }
        return xorstring;
    }
    ///<summary>Save a file, if the file already exists it will be overwritten</summary>
    public async Task<bool> SaveFile(string file, Save save)
    {
        working = true;
        //the file may be prefixed with a path, so we need to check for that
        string path = fullpath;
        if (file.Contains("\\")){
            path += file.Substring(0, file.LastIndexOf("\\") + 1);
            file = file.Substring(file.LastIndexOf("\\") + 1);
        }
        if (debug){ Log($"Saving To {path}"); }
        //check if the path exists, if not create it
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        //convert the save to serialised data
        string data = GetSaveAsRaw(save);
        //write the data to the file asynchronously
        using (StreamWriter outputFile = new StreamWriter(path + file))
        { await outputFile.WriteAsync(data); }
        if (debug) { Log($"Saved {file} to {path}"); }
        working = false;
        return true;
    }
    ///<summary>convert a save to a rawDataString</summary>
    public string GetSaveAsRaw(Save save)
    {
        //convert the save to serialised data
        string data = "";
        if (save.StringNames.Count > 0) {
            //line should look like this: dasr.{stringname}="{stringvalue}"
            for (int i = 0; i < save.StringNames.Count; i++)
            { data += $"dasr.{save.StringNames[i]}=\"{save.Strings[i]}\"\n"; }
        }
        if (save.IntNames.Count > 0) {
            //line should look like this: dain.{intname}={intvalue}
            for (int i = 0; i < save.IntNames.Count; i++)
            { data += $"dain.{save.IntNames[i]}={save.Ints[i]}\n"; }
        }
        if (save.BoolNames.Count > 0) {
            //line should look like this: dabo.{boolname}={boolvalue}
            for (int i = 0; i < save.BoolNames.Count; i++)
            { data += $"dabo.{save.BoolNames[i]}={save.Bools[i]}\n"; }
        }
        if (save.FloatNames.Count > 0) {
            //line should look like this: dafl.{floatname}={floatvalue}
            for (int i = 0; i < save.FloatNames.Count; i++)
            { data += $"dafl.{save.FloatNames[i]}={save.Floats[i]}\n"; }
        }
        //encrypt the data if needed
        if (encryptKey != null) { data = EncDecProcess(data, encryptKey); }
        return data;
    }
    ///<summary>Load a file and return the contents as a save</summary>
    public async Task<Save> LoadFile(string file)
    {
        working = true;
        //the file may be prefixed with a path, so we need to check for that
        string path = fullpath;
        if (file.Contains("\\")) {
            path += file.Substring(0, file.LastIndexOf("\\") + 1);
            file = file.Substring(file.LastIndexOf("\\") + 1);
        }
        //check if the file exists, if it doesn't return null
        if (!File.Exists(path + file)) {
            if(debug) { Log($"File {file} not found in {path}"); }
            working = false;
            return null;
        }
        if (debug) { Log($"Loading {file} from {path}"); }
        //read the file asynchronously
        string data = await GetStringFromFileNETAgnostic(path + file);
        if (debug) { Log($"Content: {data}"); }
        //convert the data to a save
        Save save = LoadSaveFromRaw(data);
        working = false;
        if (debug) { Log($"Loaded {file}"); }
        return save;
    }
    ///<summary>convert a rawDataString to a save</summary>
    public Save LoadSaveFromRaw(string content) {
        if (debug) { Log($"Content: {content}"); }
        //decrypt the data if needed
        if (encryptKey != null) { content = EncDecProcess(content, encryptKey); }
        //convert the data to a save
        //the data is split into sections, so we need to split it up
        //check the content of the first line
        string[] lines = content.Split(splitSettings, StringSplitOptions.RemoveEmptyEntries);
        if (debug) { Log($"Parsing[{lines.Length}]"); }
        Save save = new Save();
        string[] line; string name;
        for (int i = 0; i < lines.Length; i++) {
            try {
                line = lines[i].Split('='); name = line[0].Substring(5);
                if (lines[i].StartsWith("dasr.")) {
                    string value = line[1].Substring(1, line[1].Length - 2);
                    save.SetString(name, value);
                }
                else if (lines[i].StartsWith("dain.")) {
                    int value = int.Parse(line[1]);
                    save.SetInt(name, value);
                }
                else if (lines[i].StartsWith("dabo.")) {
                    bool value = bool.Parse(line[1]);
                    save.SetBool(name, value);
                }
                else if (lines[i].StartsWith("dafl.")) {
                    float value = float.Parse(line[1]);
                    save.SetFloat(name, value);
                }
            }
            catch {
                if (debug) { Log($"ParseError on line: {i}"); }
            }
        }
        if (debug) { Log("Content Parsed"); }
        return save;
    }
    ///<summary>Save a text file, if the file already exists it will be overwritten</summary>
    public async Task SaveText(string file, string text, bool forcePath = false)
    {
        string path = "";
        if (!forcePath) path = fullpath;
        if (file.Contains("\\"))
        {
            path += file.Substring(0, file.LastIndexOf("\\") + 1);
            file = file.Substring(file.LastIndexOf("\\") + 1);
        }
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        //encrypt the data if needed
        if (encryptKey != null) { text = EncDecProcess(text, encryptKey); }
        using (StreamWriter outputFile = new StreamWriter(path + file))
        { await outputFile.WriteAsync(text); }
    }
    ///<summary>Load a text file and return the contents as a string array</summary>
    public async Task<string[]> LoadText(string file, bool forcePath = false, bool clearBlankLines = true)
    {
        string path = "";
        if (!forcePath) path = fullpath;
        if (file.Contains("\\")) {
            path += file.Substring(0, file.LastIndexOf("\\") + 1);
            file = file.Substring(file.LastIndexOf("\\") + 1);
        }
        if (!File.Exists(path + file)) return null;
        string content = await GetStringFromFileNETAgnostic(path + file);
        //encrypt the data if needed
        if (encryptKey != null) { content = EncDecProcess(content, encryptKey); }
        StringSplitOptions options = clearBlankLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;
        return content.Split(splitSettings, options);
    }
    ///<summary>Get a list of all the files in a folder</summary>
    public string[] GetFilesInFolder(string folder, bool forcePath = false){
        string path = "";
        if (!forcePath) path = fullpath;
        if (folder.Contains("\\")) {
            path += folder.Substring(0, folder.LastIndexOf("\\") + 1);
            folder = folder.Substring(folder.LastIndexOf("\\") + 1);
        }
        Log(path + folder);
        if (!Directory.Exists(path + folder)) return null;
        return Directory.GetFiles(path + folder);
    }
    ///<summary>Delete a file</summary>
    public bool DeleteFile(string file, bool forcePath = false){
        string path = "";
        if (!forcePath) path = fullpath;
        if (file.Contains("\\")) {
            path += file.Substring(0, file.LastIndexOf("\\") + 1);
            file = file.Substring(file.LastIndexOf("\\") + 1);
        }
        if (!File.Exists(path + file)) return false;
        File.Delete(path + file);
        return true;
    }
    ///<summary>Delete a folder and all its contents</summary>
    public bool DeleteFolder(string folder, bool forcePath = false){
        string path = "";
        if (!forcePath) path = fullpath;
        if (folder.Contains("\\")) {
            path += folder.Substring(0, folder.LastIndexOf("\\") + 1);
            folder = folder.Substring(folder.LastIndexOf("\\") + 1);
        }
        if (!Directory.Exists(path + folder)) return false;
        Directory.Delete(path + folder, true);
        return true;
    }
    ///<summary>Framework File reader for compatibility</summary>
    string FrameworkReadAllText(string filePath){
        string fileContents = string.Empty;
        try{
            // Open the file using a stream reader.
            using (StreamReader streamReader = new StreamReader(filePath)){
                fileContents = streamReader.ReadToEnd(); // Read the contents of the file into a string variable.
            }
        }
        catch (Exception ex) {
            // Handle any exceptions that occur while attempting to read the file.
            Log("An error occurred: " + ex.Message);
        }
        return fileContents;
    }
    void Log(string message){
        #if UNITY_EDITOR
            UnityEngine.Debug.Log(message);
        #else
            Console.WriteLine(message);
        #endif
    }
    async Task<string> GetStringFromFileNETAgnostic(string file){
        string content = "";
        #if NETCOREAPP
            content = await File.ReadAllTextAsync(file);
        #else
            content = FrameworkReadAllText(file);
        #endif
        return content;
    }
}