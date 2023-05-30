using System;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using TMPro;
using UnityEngine.UI;
using Firebase;
using Firebase.Storage;
using Firebase.Extensions;//ContinueWithOnMainThread
using System.Threading.Tasks;

public class FirebaseStorageSaveAndLoad : MonoBehaviour
{
    [SerializeField] private Transform objectsSpace;
    [SerializeField] private Transform pointLight;
    [SerializeField] private Transform spotLight;
    [SerializeField] private Transform objParentObject;
    [SerializeField] private Transform objchildObject;
    [SerializeField] private Transform room1;
    [SerializeField] private Transform room2;

    private string sceneDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/DataFolder/SceneData";
    private string allObjectsDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/DataFolder/AllObjectsData";

    private StorageReference storageRef = null;
    private StorageReference sceneJsonRef = null;
    private StorageReference allObjectsjsonRef = null;

    private Transform loadSceneObjectTransform;
    private TransformData tfd;
    private int objDataCount = 0;//read �� �� allObjectsData �ε��� ���� ����ϱ����ؼ�. LoadScene() ���� ���
    private AllObjectsData readAllObjectsData;

    [SerializeField] private List<OBJ_DataCustomParsing> allObjectsData = new List<OBJ_DataCustomParsing>();//�ν����� Ȯ�ο�. ���� ���� X
    // Start is called before the first frame update
    void Start()
    {
        string sceneDataJson = File.ReadAllText(sceneDataPath);
        tfd = JsonUtility.FromJson<TransformData>(sceneDataJson);
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;
        storageRef = storage.GetReferenceFromUrl("YourURL");
        sceneJsonRef = storageRef.Child("TestFolder/SceneData");
        allObjectsjsonRef = storageRef.Child("TestFolder/allObjectsData");
    }
    public async void SaveButtonClick()
    {
        string result = await SaveLocal_IntegratedAllJson();
        Debug.Log(result);
        FirebaseSaveJson();
    }
    public async Task<string> SaveLocal_IntegratedAllJson()//OBJ������ �Ľ̽�Ű�� ������ JSON������ �����Ǵµ� ������ ȣ����� �ּ�ȭ�ϱ����ؼ� �ϳ��� ����.
    {
        string json = "";
        for (int i = 0; i < tfd.myName.Count; i++)
        {
            if(tfd.path[i].Substring(0, 17) != "ThisIsInnerObject")
            {
                json = File.ReadAllText(tfd.path[i]);
                allObjectsData.Add(JsonUtility.FromJson<OBJ_DataCustomParsing>(json));
            }
        }
        AllObjectsData allData = new AllObjectsData(allObjectsData);
        string allJson = JsonUtility.ToJson(allData);
        File.WriteAllText(allObjectsDataPath, allJson);
        File.WriteAllBytes(allObjectsDataPath + "GZip", SaveCompressedJsonToFile(allJson));
        await Task.Delay(2000);
        return "Local Save Success";
    }
    public void ReadStorageJson()
    {
        string json = "";
        sceneJsonRef.GetBytesAsync(long.MaxValue).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to download image: " + task.Exception);
                return;
            }
            else
            {
                byte[] sceneData = task.Result;
                json = Encoding.UTF8.GetString(sceneData);
                tfd = JsonUtility.FromJson<TransformData>(json);
                RoomLoad();
                ReadAllObjectsData();
            }
        });
    }
    private void RoomLoad()
    {
        if(tfd.myRoomName == "Room1")
        {
            Instantiate(room1, objectsSpace);
        }
        else if (tfd.myRoomName == "Room2")
        {
            Instantiate(room2, objectsSpace);
        }
    }
    private void ReadAllObjectsData()
    {
        string json = "";
        allObjectsjsonRef.GetBytesAsync(long.MaxValue).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to download image: " + task.Exception);
                return;
            }
            else
            {
                byte[] objectsData = task.Result;
                json = LoadCompressedJsonFromFile(objectsData);
                readAllObjectsData = JsonUtility.FromJson<AllObjectsData>(json);
                LoadObjects();
            }
        });
    }
    private void LoadObjects()
    {
        int objCount = tfd.myName.Count;//obj������Ʈ ����.����Ʈ ������Ʈ�� ����.
        for(int i = 0; i < objCount; i++)
        {
            LoadScene(tfd.path[i], i);
        }
    }
    private void FirebaseSaveJson()
    {
        allObjectsjsonRef.PutFileAsync(allObjectsDataPath + "GZip").ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("File uploaded successfully.");
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Failed to upload file: " + task.Exception);
            }
        });
        sceneJsonRef.PutFileAsync(sceneDataPath).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("File uploaded successfully.");
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Failed to upload file: " + task.Exception);
            }
        });
    }
    private void LoadScene(string path, int tfd_Index)//loadSceneObjectTransform�� ������Ʈ�� �Ҵ��ϴ� ������ ������Ʈ�� �پ��ִ� ObjectSceneData��ũ��Ʈ�� �����ϱ� ���ؼ��̴�.
    {//ObjectSceneData���� ���� ������Ʈ�� ������ ����ִ�.
        if (path.Substring(0, 17) == "ThisIsInnerObject")
        {
            if (path == "ThisIsInnerObject_PointLight")
            {
                loadSceneObjectTransform = Instantiate(pointLight, objectsSpace);//room �ȿ� �ִ´�.
            }
            else if (path == "ThisIsInnerObject_SpotLight")
            {
                loadSceneObjectTransform = Instantiate(spotLight, objectsSpace);//room �ȿ� �ִ´�.
            }
        }
        else
        {
            int verticesAdd = 0;
            int normalsAdd = 0;
            int uvsAdd = 0;
            int trianglesAdd = 0;

            OBJ_DataCustomParsing objData = readAllObjectsData.allObjectsData[objDataCount];
            objDataCount++;

            int childCount = objData.obj_Name.Count - 1;// parent�̸��� ���ԵǾ������Ƿ�
            Transform cloneParent = Instantiate(objParentObject, objectsSpace);
            loadSceneObjectTransform = cloneParent;
            cloneParent.name = objData.obj_Name[0];
            //cloneParent.GetComponent<ObjectSceneData>().path = path;
            for (int i = 0; i < childCount; i++)
            {
                Transform cloneChild = Instantiate(objchildObject, cloneParent);
                Mesh childMesh = cloneChild.GetComponent<MeshFilter>().mesh;
                
                cloneChild.name = objData.obj_Name[i + 1];
                //2���� �迭 �Ǵ� ����Ʈ�� �Ϲ������� json���� ��ȯ �Ұ����ϹǷ� child�� ���ؽ�,uv, Ʈ���� �ޱ��� ����Ʈ �ϳ��� ����� ������ŭ �������� �����ϴ� ������
                childMesh.vertices = objData.obj_Vertices.GetRange(verticesAdd, objData.child_VerticesCount[i]).ToArray();
                childMesh.normals = objData.obj_Normals.GetRange(normalsAdd, objData.child_NormalsCount[i]).ToArray();
                childMesh.uv = objData.obj_Uvs.GetRange(uvsAdd, objData.child_UVCount[i]).ToArray();
                childMesh.triangles = objData.obj_Polygon.GetRange(trianglesAdd, objData.child_TrianglesCount[i]).ToArray();

                verticesAdd += objData.child_VerticesCount[i];
                normalsAdd += objData.child_NormalsCount[i];
                uvsAdd += objData.child_UVCount[i];
                trianglesAdd += objData.child_TrianglesCount[i];

                if (objData.obj_Texture[i] != "null")
                {
                    cloneChild.GetComponent<Renderer>().material.mainTexture = Decoding(objData.obj_Texture[i]);
                }
                cloneChild.GetComponent<Renderer>().material.color = objData.obj_color32[i];
                //childMesh.RecalculateNormals();
            }
        }//StartSceneLoadingObjects()�� json�� ����ִ� ������, �����̼�, ������ ���� �־��ָ� �׿� �°� transform�� ����ȴ�.
        loadSceneObjectTransform.GetComponent<ObjectSceneData>().StartSceneLoadingObjects(tfd.myName[tfd_Index], tfd.myPosition[tfd_Index], tfd.myRotation[tfd_Index], tfd.myScale[tfd_Index], tfd.myProductExplanation[tfd_Index], tfd.path[tfd_Index]);
    }
    private Texture2D Decoding(string encodedValue)//
    {
        // PNG ��Ʈ�� ���� ����Ʈ �迭�� ���ڵ�
        byte[] bytes = System.Convert.FromBase64String(encodedValue);

        // ����Ʈ �迭�� �ؽ�ó�� ��ȯ
        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(bytes);

        // �ؽ�ó ���
        return texture;
    }
    private byte[] SaveCompressedJsonToFile(string jsonData)
    {
        byte[] compressedData;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                using (StreamWriter writer = new StreamWriter(gzipStream))
                {
                    writer.Write(jsonData);
                }
            }
            compressedData = memoryStream.ToArray();//����� byte[] ������
        }
        Debug.Log("Compressed JSON Data saved to file");
        return compressedData;

    }
    private string LoadCompressedJsonFromFile(byte[] zipData)
    {
        byte[] compressedData = zipData;

        using (MemoryStream memoryStream = new MemoryStream(compressedData))
        {
            using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                using (StreamReader reader = new StreamReader(gzipStream))//StreamReader : Ư�� ���ڵ��� ����Ʈ ��Ʈ������ ���ڸ� �д� TextReader �� ����.
                {
                    string jsonData = reader.ReadToEnd();
                    return jsonData;
                }
            }
        }
    }
}
    
    

