///FileSuperSystem V2.5

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using BinaryFormatter = System.Runtime.Serialization.Formatters.Binary.BinaryFormatter;
using System.Diagnostics;
/// <summary>
/// This is the Save class, it contains all the data that will be saved
/// </summary>
public class Save
{
    bool isPlain;
    public Dictionary<string, object> data;
    public Save(){ //default constructor
        this.data = new Dictionary<string, object>();
        this.isPlain = true;
    }
    public Save(Dictionary<string, object> data){ //internal constructor
        this.data = new Dictionary<string, object>();
        this.isPlain = true;
        foreach(KeyValuePair<string, object> pair in data){
            SetVar(pair.Key, pair.Value);
        }
    }
    public bool isPlainData(){ return isPlain; } //check if the data is plain(can be saved as plain text)
    public Dictionary<string, object> GetData(){ return data; } //get the data dictionary
    /// <summary>
    /// Set a variable in the save, if the variable already exists it will be overwritten
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <exception cref="ArgumentException"></exception>
    public void SetVar(string name, object value){
        if(value == null){ return; }
        //check if the object is serializable
        if (!value.GetType().IsSerializable)
        {
            throw new ArgumentException($"Type {value.GetType().Name} is not serializable");
        }
        //if value is not string, int, float or bool, set isPlain to false
        Type t = value.GetType();
        if (t != typeof(string) && t != typeof(int) && t != typeof(float) && t != typeof(bool))
        { isPlain = false; }

        if(data.ContainsKey(name)){ data[name] = value; }
        else{ data.Add(name, value); }
    }
    /// <summary>
    /// Get a variable from the save, if it doesn't exist it will be created with the default value
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public T GetVar<T>(string name, T defaultValue){
        if(data.ContainsKey(name)){ return (T)data[name]; }
        else{ SetVar(name, defaultValue); return defaultValue; }
    }
    /// <summary>
    /// Try to get a variable from the save, if it doesn't exist it will return false
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool TryGetVar<T>(string name, out T value){
        if(data.ContainsKey(name)){ value = (T)data[name]; return true; }
        else{ value = default; return false; }
    }
    /// <summary>
    /// Get all the keys in the save
    /// </summary>
    /// <returns></returns>
    public string[] GetKeys(){
        return data.Keys.ToArray();
    }
    /// <summary>
    /// Check if the save contains a key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool ContainsKey(string key){
        return data.ContainsKey(key);
    }
    /// <summary>
    /// Remove a key from the save
    /// </summary>
    /// <param name="key"></param>
    public void RemoveKey(string key){
        if(data.ContainsKey(key)){ data.Remove(key); }
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
    BinaryFormatter bf = new BinaryFormatter();
    ///<summary>Create a new FileSuper object</summary>
    public FileSuper(string project, string studio, bool debug = false, string[] splitSettings = null)
    {
        this.debug = debug;
        this.encryptKey = null;
        if(splitSettings != null){ this.splitSettings = splitSettings;}
        else{ this.splitSettings = new string[] { "\n", "\r\n" };}
        fullpath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\{studio}\\{project}\\";
        //use the persistent data path for android
        #if UNITY_ANDROID
            fullpath = $"{UnityEngine.Application.persistentDataPath}\\";
        #endif
    }
    ///<summary>Set the encryption key, null = no encryption</summary>
    public void SetEncryption(string key = null) { encryptKey = key; }
    ///<summary>Encrypt or decrypt a string using a key</summary>
    public string EncDecProcess(string dataIn, string key) {
        string xorstring = "", input = dataIn, enckey = key;
        for (int i = 0; i < input.Length; i++)
        { xorstring += (char)(input[i] ^ enckey[i % enckey.Length]); }
        return xorstring;
    }
    ///<summary>Save a file, if the file already exists it will be overwritten</summary>
    public async Task<bool> SaveFile(string file, Save save, bool forcePath = false) {
        working = true;
        //the file may be prefixed with a path, so we need to check for that
        string path = "";
        if (!forcePath) path = fullpath;
        if (file.Contains("\\")){
            path += file.Substring(0, file.LastIndexOf("\\") + 1);
            file = file.Substring(file.LastIndexOf("\\") + 1);
        }
        if (debug){ Log($"Saving To {path}"); }
        //check if the path exists, if not create it
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        //convert the save to serialised data
        bool plain = save.isPlainData();
        string data = plain ? GetSaveAsString(save) : Convert.ToBase64String(GetSaveAsBytes(save));
        //encrypt the data if needed
        if (encryptKey != null) { data = EncDecProcess(data, encryptKey); }
        //write the data to the file asynchronously
        using (StreamWriter outputFile = new StreamWriter(path + file)) { 
            await outputFile.WriteAsync(data);  
        }
        if (debug) { Log($"Saved {file} to {path}"); }
        working = false;
        return true;
    }
    ///<summary>convert a save to a rawDataString</summary>
    public byte[] GetSaveAsBytes(Save save) { //This is used for complex data, classes ect
        byte[] data = ObjectToByteArray(save.GetData());
        return data;
    }
    public string GetSaveAsString(Save save) { //this is used for simple types, can be user edited
        string strdata = "";
        Dictionary<string, object> saveData = save.GetData();
        for(int i = 0; i < save.data.Count; i++){
            KeyValuePair<string, object> pair = saveData.ElementAt(i);
            var value = pair.Value;
            string name = pair.Key;
            if (value.GetType() == typeof(string)) { strdata += $"s:{name}:{value}\n"; }
            else if (value.GetType() == typeof(int)) { strdata += $"i:{name}:{value}\n"; }
            else if (value.GetType() == typeof(float)) { strdata += $"f:{name}:{value}\n"; }
            else if (value.GetType() == typeof(bool)) { strdata += $"b:{name}:{value}\n"; }
        }
        strdata = strdata.Substring(0, strdata.Length - 1); //remove the last newline
        if(debug){Log($"saving: {strdata}");}
        return strdata;
    }
    ///<summary>Load a file and return the contents as a save</summary>
    public async Task<Save> LoadFile(string file, bool forcePath = false) {
        working = true;
        //the file may be prefixed with a path, so we need to check for that
        string path = "";
        if (!forcePath) path = fullpath;
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
        string data = await GetDataFromFileNETAgnostic(path + file);
        //convert the data to a save
        Save save = StringToSave(data);
        working = false;
        if (debug) { Log($"Loaded {file}"); }
        return save;
    }
    ///<summary>convert a rawDataString to a save</summary>
    public Save StringToSave(string content) {
        //decrypt the data if needed
        if (encryptKey != null) { content = EncDecProcess(content, encryptKey); }
        //convert the data to a save
        Save save;
        try{
            byte[] data = Convert.FromBase64String(content);
            Dictionary<string, object> saveData = ByteArrayToObject(data) as Dictionary<string, object>;
            save = new Save(saveData);
        }catch{
            try{
                save = new Save();
                string[] lines = content.Split(splitSettings, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines) {
                    string[] parts = line.Split(':');
                    if (parts.Length < 3) { continue; }
                    string type = parts[0], name = parts[1];
                    //rejoin any split values after this
                    string value = string.Join(":", parts, 2, parts.Length - 2);
                    if (type == "s") { save.SetVar(name, value); }
                    else if (type == "i") { save.SetVar(name, int.Parse(value)); }
                    else if (type == "f") { save.SetVar(name, float.Parse(value)); }
                    else if (type == "b") { save.SetVar(name, bool.Parse(value)); }
                }
            }catch{
                save = null;
                Log("Error parsing save data");
            }
            
        }
        return save;
    }
    ///<summary>Save a text file, if the file already exists it will be overwritten</summary>
    public async Task SaveText(string file, string text, bool forcePath = false) {
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
    public async Task<string[]> LoadText(string file, bool forcePath = false, bool clearBlankLines = true) {
        string path = "";
        if (!forcePath) path = fullpath;
        if (file.Contains("\\")) {
            path += file.Substring(0, file.LastIndexOf("\\") + 1);
            file = file.Substring(file.LastIndexOf("\\") + 1);
        }
        if (!File.Exists(path + file)) return null;
        string content = await GetDataFromFileNETAgnostic(path + file);
        //encrypt the data if needed
        if (encryptKey != null) { content = EncDecProcess(content, encryptKey); }
        StringSplitOptions options = clearBlankLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;
        return content.Split(splitSettings, options);
    }
    ///<summary>Get a list of all the files in a folder</summary>
    public string[] GetFilesInFolder(string folder, bool forcePath = false) {
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
    public bool DeleteFile(string file, bool forcePath = false) {
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
    public bool DeleteFolder(string folder, bool forcePath = false) {
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
    string FrameworkReadFile(string filePath) {
        string fileContents = "";
        try{
            // Open the file using a stream reader.
            using (StreamReader streamReader = new StreamReader(filePath)){
                fileContents = streamReader.ReadToEnd(); // Read the contents of the file into a byte array.
            }
        }
        catch (Exception ex) {
            // Handle any exceptions that occur while attempting to read the file.
            Log("An error occurred: " + ex.Message);
        }
        return fileContents;
    }
    byte[] ObjectToByteArray(object b) { //convert an object to a byte array
        try{
            if(b == null){ return null; }
            using (MemoryStream ms = new MemoryStream())
            { bf.Serialize(ms, b); return ms.ToArray(); }
        }
        catch(Exception e){
            Log($"Error converting object to byte array: {e.Message}");
            return null;
        }
    }
    object ByteArrayToObject(byte[] arrBytes) { //convert a byte array to an object
        try{
            using (MemoryStream memStream = new MemoryStream()) {
                memStream.Write(arrBytes, 0, arrBytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                return bf.Deserialize(memStream);
            }
        }
        catch(Exception e){
            //this log is commented because we are intentionally trying to parse the data as a string if it fails
            //Log($"Error converting byte array to object: {e.Message}");
            return null;
        }
    }
    void Log(string message){
        #if UNITY_EDITOR
                UnityEngine.Debug.Log(message);
        #else
            Console.WriteLine(message);
            Debug.WriteLine(message);
        #endif
    }
    async Task<string> GetDataFromFileNETAgnostic(string file){
        string content;
        #if NETCOREAPP
            content = await File.ReadAllTextAsync(file);
        #elif UNITY_ANDROID
            content = await File.ReadAllTextAsync(file);
        #else
            content = FrameworkReadFile(file);
        #endif
        return content;
    }
}