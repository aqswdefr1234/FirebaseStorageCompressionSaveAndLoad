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
    private int objDataCount = 0;//read 할 때 allObjectsData 인덱스 값을 계산하기위해서. LoadScene() 에서 사용
    private AllObjectsData readAllObjectsData;

    [SerializeField] private List<OBJ_DataCustomParsing> allObjectsData = new List<OBJ_DataCustomParsing>();//인스펙터 확인용. 직접 연결 X
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
    public async Task<string> SaveLocal_IntegratedAllJson()//OBJ파일을 파싱시키면 개별의 JSON파일이 생성되는데 서버의 호출수를 최소화하기위해서 하나로 통합.
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
        int objCount = tfd.myName.Count;//obj오브젝트 개수.라이트 오브젝트는 포함.
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
    private void LoadScene(string path, int tfd_Index)//loadSceneObjectTransform에 오브젝트를 할당하는 이유는 오브젝트에 붙어있는 ObjectSceneData스크립트에 접근하기 위해서이다.
    {//ObjectSceneData에는 개별 오브젝트의 정보가 담겨있다.
        if (path.Substring(0, 17) == "ThisIsInnerObject")
        {
            if (path == "ThisIsInnerObject_PointLight")
            {
                loadSceneObjectTransform = Instantiate(pointLight, objectsSpace);//room 안에 넣는다.
            }
            else if (path == "ThisIsInnerObject_SpotLight")
            {
                loadSceneObjectTransform = Instantiate(spotLight, objectsSpace);//room 안에 넣는다.
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

            int childCount = objData.obj_Name.Count - 1;// parent이름도 포함되어있으므로
            Transform cloneParent = Instantiate(objParentObject, objectsSpace);
            loadSceneObjectTransform = cloneParent;
            cloneParent.name = objData.obj_Name[0];
            //cloneParent.GetComponent<ObjectSceneData>().path = path;
            for (int i = 0; i < childCount; i++)
            {
                Transform cloneChild = Instantiate(objchildObject, cloneParent);
                Mesh childMesh = cloneChild.GetComponent<MeshFilter>().mesh;
                
                cloneChild.name = objData.obj_Name[i + 1];
                //2차원 배열 또는 리스트는 일반적으로 json으로 변환 불가능하므로 child의 버텍스,uv, 트라이 앵글을 리스트 하나로 만들어 개수만큼 범위에서 추출하는 방법사용
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
        }//StartSceneLoadingObjects()에 json에 담겨있는 포지션, 로테이션, 스케일 값을 넣어주면 그에 맞게 transform이 변경된다.
        loadSceneObjectTransform.GetComponent<ObjectSceneData>().StartSceneLoadingObjects(tfd.myName[tfd_Index], tfd.myPosition[tfd_Index], tfd.myRotation[tfd_Index], tfd.myScale[tfd_Index], tfd.myProductExplanation[tfd_Index], tfd.path[tfd_Index]);
    }
    private Texture2D Decoding(string encodedValue)//
    {
        // PNG 스트링 값을 바이트 배열로 디코딩
        byte[] bytes = System.Convert.FromBase64String(encodedValue);

        // 바이트 배열을 텍스처로 변환
        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(bytes);

        // 텍스처 출력
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
            compressedData = memoryStream.ToArray();//압축된 byte[] 데이터
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
                using (StreamReader reader = new StreamReader(gzipStream))//StreamReader : 특정 인코딩의 바이트 스트림에서 문자를 읽는 TextReader 를 구현.
                {
                    string jsonData = reader.ReadToEnd();
                    return jsonData;
                }
            }
        }
    }
}
    
    

