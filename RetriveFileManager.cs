using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Assets.SimpleZip;
using System.Reflection;
using Firebase.Storage;
using System.Threading.Tasks;
using Firebase.Extensions;

///<summary>
///manage for the retrive file manager
///and select for to retrive the file managers
///</summary>
public class RetriveFileManager : MonoBehaviour
{
    //input fiele text
    public InputField inputFieldSystem;

    //get the URL string for the WINZIP
    public string downloadURL;

    //get the model for the english smalls
    public string savePath;

    //get to have the unity requested
    public UnityWebRequest unityRequest;

    //give the text progress debug
    public Text progressText;

    //give the news text for the progress text
    public Text newsTest;

    //get the patch button
    public Button button;

    //get the object of the close button
    public GameObject closeButton;

    //define for the file persistence
    //public string FilePersistent;

    //define for the file reference to zip
    string fileToZip;

    //define the inner scroll bar
    public Image innerScrollBar;

    //define to copy to streaming asset completed
    bool copyToStreamingAssetCompleted;

    //next success condition
    bool nextSuccessCondition;

    static RetriveFileManager retriveFileManager;

    string currentDir;

    StorageReference storageReference;

    StorageReference textureStorageReference2;

    private void Awake()
    {
        currentDir = System.AppDomain.CurrentDomain.BaseDirectory;

        retriveFileManager = this;
    }

    private void Start()
    {
        GoogleCloudFirebaseGetReferenceURL();
    }

    void Update()
    {
        //savePath = inputFieldSystem.text;

        progressText.text = GetProcess().ToString();

        innerScrollBar.fillAmount = GetProcess();
    }

    public void GoogleCloudFirebaseGetReferenceURL ()
    {
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;

        // Create a storage reference from our storage service
        storageReference = storage.GetReferenceFromUrl("gs://mmoprivateserver-af6da.appspot.com/updateren.zip");

        //create a storage reference for image textures
        //LATER

        return;
    }

    public void StartPatch ()
    {
        // Fetch the download URL
        storageReference.GetDownloadUrlAsync().ContinueWithOnMainThread(task => {
            if (!task.IsFaulted && !task.IsCanceled)
            {
                Debug.Log("Download URL: " + task.Result);

                string url = task.Result.ToString();

                // ... now download the file via WWW or UnityWebRequest.
                string pathCheckToImagineClient = Path.Combine(currentDir, "ImagineClient.exe"); 

                //check the file if ImagineClient.exe is on the local directory
                if(File.Exists(pathCheckToImagineClient))StartCoroutine(DownloadFileOperation(url));
                else newsTest.text = "Unable to locate ImagineClient.exe client file executables";
            }
            else
            {
                Debug.Log(task.Exception.ToString());
            }

        });

        //set the to make the apps stay awake
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //set the file persistent exe
        //FilePersistent = Path.Combine(currentDir, "ImaginePatch");

        //get the current directory path
        savePath = Path.Combine(currentDir, "");

        //download file from source
    }

    ///<summary>
    ///manage for the file operation
    ///and select for the file to downloads
    ///</summary>
    IEnumerator DownloadFileOperation(string URL)
    {
        //appear the news
        newsTest.text = "Downloading the Client Updater File";

        //disable the button
        button.enabled = false;

        //send request to the URL
        unityRequest = UnityWebRequest.Get(URL);                                                      // URL

        unityRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();                  // get for the download handler buffer

        unityRequest.timeout = 0;                                                                     // set the timeout, if webRequest.SendWebRequest () returns a connection timeout, and is true isNetworkError

        yield return unityRequest.SendWebRequest();                                                   // sent for the web requested    

        //check for the connection error
        if (unityRequest.result == UnityWebRequest.Result.ConnectionError)
        {
            //appear the news
            newsTest.text = "Your Connection is Shit , check your connection";

            nextSuccessCondition = false;

            button.enabled = true;
        }

        //check for the protocol error
        else if (unityRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            //appear the news
            newsTest.text = "Adress is not found";

            nextSuccessCondition = false;

            button.enabled = true;
        }

        //check for the processing data error
        else if (unityRequest.result == UnityWebRequest.Result.DataProcessingError)
        {
            //appear the news
            newsTest.text = "Data Transfer Error";

            nextSuccessCondition = false;

            button.enabled = true;
        }

        //if zip is completed
        else
        {
            // Get the binary data
            var fileData = unityRequest.downloadHandler.data;

            //check the file exitss
            fileToZip = Path.Combine(currentDir, "process_patch.zip");

            //create the file into zip
            File.Create(fileToZip).Dispose();

            //set to create the zip bytes
            File.WriteAllBytes(fileToZip, fileData);

            //next success condiion true
            nextSuccessCondition = true;
        }

        //wait for next success condition
        yield return new WaitUntil(() => nextSuccessCondition);

        //wait for the download handler is done
        yield return new WaitUntil(() => unityRequest.downloadProgress >= 1.0f);

        //appear the news
        newsTest.text = "Extracting Client Updater File";

        //wait until the zip has file
        yield return new WaitUntil(() => File.Exists(fileToZip));

        //read all bytess
        byte[] bytes = File.ReadAllBytes(fileToZip);

        //unpack the zip folder
        TestUnpackZip(bytes);

        //give wait when file exists as file
        //yield return new WaitUntil(() => Directory.Exists(FilePersistent));

        //start for to copy streaming asset completed
        yield return new WaitUntil(() => copyToStreamingAssetCompleted);

        //appear the news
        newsTest.text = "Patch Completed";

        //give the wait for second realtime 0.2
        yield return new WaitForSecondsRealtime(0.2f);

        //break the yield
        yield break;
    }

    /// <summary>
    /// Get the download progress
    /// </summary>
    /// <returns></returns>
    public float GetProcess()
    {
        //check if not unity request updater
        if (unityRequest != null)
        {
            //set the download progress
            return unityRequest.downloadProgress;
        }

        return 0;
    }

    /// <summary>
    // Get the current length downloads
    /// </summary>
    /// <returns></returns>
    public long GetCurrentLength()
    {
        if (unityRequest != null)
        {
            //set the download bytes
            return (long)unityRequest.downloadedBytes;
        }

        return 0;
    }

    ///<summary>
    ///manage to unpack zip
    ///and select for the zip packs
    ///</summary>
    public void TestUnpackZip(byte[] fileData)
    {
        string zipPath = Path.Combine(currentDir, "process_patch.zip");         //set the zip path

        string extractPath = savePath;                                                              //define for the save path


        if (UnZip(zipPath, extractPath))
        {
            copyToStreamingAssetCompleted = true;                                                     //complete

            File.Delete(zipPath);

            //appear the news
            newsTest.text = "Patch Completed";

            closeButton.SetActive(true);
        }

        return;
    }

    /// <summary>
    /// manage to exit the game programs
    /// </summary>
    public void ExitProgram ()
    {
        Application.Quit();

        return;
    }

    ///<summary>
    ///manage to unzip selection
    ///and select for the zip path convertion
    ///</summary>
    public static bool UnZip(string FileToUpZip, string ZipedFolder)
    {
        if (!File.Exists(FileToUpZip))
        {
            Debug.LogError("UnZip Is Not Exists !!");
            return false;
        }

        if (!Directory.Exists(ZipedFolder))
        {
            Directory.CreateDirectory(ZipedFolder);
        }

        ICSharpCode.SharpZipLib.Zip.ZipInputStream s = null;
        ICSharpCode.SharpZipLib.Zip.ZipEntry theEntry = null;

        string fileName;
        try
        {

            //Encoding gbk = Encoding.GetEncoding("gbk"); // Prevent Chinese name from garbled  
            //Debug.Log(gbk);
            ICSharpCode.SharpZipLib.Zip.ZipConstants.DefaultCodePage = System.Text.Encoding.GetEncoding("UTF-8").CodePage;
            using (FileStream fsteam = File.OpenRead(FileToUpZip))
            {
                s = new ICSharpCode.SharpZipLib.Zip.ZipInputStream(File.OpenRead(FileToUpZip));

                while ((theEntry = s.GetNextEntry()) != null)
                {

                    if (theEntry.Name != "")
                    {
                        fileName = Path.Combine(ZipedFolder, theEntry.Name);
                        ///Judge whether the file path is a folder

                        if (fileName.EndsWith("/") || fileName.EndsWith("\\"))
                        {
                            Directory.CreateDirectory(fileName);
                            continue;
                        }


                        using (FileStream streamWriter = File.Create(fileName))
                        {
                            int size = 4096;
                            byte[] data = new byte[4096];
                            while (true)
                            {
                                size = s.Read(data, 0, data.Length);

                                if (size > 0)
                                    streamWriter.Write(data, 0, size);
                                else
                                    break;
                            }
                            streamWriter.Close();
                        }
                    }
                }
                fsteam.Close();
            }
            return true;
        }
        catch (System.Exception ex)
        {
            RetriveFileManager.retriveFileManager.newsTest.text = ex.Message;
            Debug.Log("UnZip Exception : " + ex.Message);
            return false;
        }
        finally
        {
            if (theEntry != null)
            {
                theEntry = null;
            }
            if (s != null)
            {
                s.Close();
                s = null;
            }
        }
    }
}
