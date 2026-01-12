using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;



[Serializable]

public class SerialPortManager1 : MonoBehaviour
{
    public static SerialPortManager1 Instance { get; private set; }
    [SerializeField] private CustomSPManager customSPManager;
    public NotePlayerSynced notePlayer;
    PortJson portJson = new PortJson();


    SerialPort serialPort;
    private CancellationTokenSource cancellationTokenSource; // CancellationTokenSource 추가
    private StringBuilder serialBuffer = new StringBuilder();
    private Queue<string> dataQueue = new Queue<string>();

    public TMP_Text sendMessage;
    public PresetData[] strongDatas;
    public PresetData[] tempDatas;

    private int tempoIndex =0;
    private int strongIndex =0;
    private int instrumentIndex =0;
    private int bpmIndex =0;
    private float[] defaultStrong = new float[32];
    private string cashingStrong= "S2";

   
    //bool isReconnecting = false;

    //public event Action OnConnected;
    //public event Action OnDisconnected;
    //private bool isConnected = false;
    protected virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);

        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    protected virtual void Start()
    {
        //OnConnected += () =>
        //{
        //    Debug.Log(">>> 연결됨 이벤트!");
        //};

        //OnDisconnected += () =>
        //{
        //    Debug.Log(">>> 연결 끊김 이벤트!");
        //};
        for (int i = 0; i < defaultStrong.Length; i++)
        {
            defaultStrong[i] = 2;
        }
        ReceivedData("M2");
        ReceivedData("S2");
        ReceivedData("T2");
        ReceivedData("B2");
        //StartCoroutine(delay_co());
        // 포트 열기
        portJson = JsonManager.instance.portJson1;
        Debug.Log($"포트 데이터 로드됨: COM={portJson.com}, Baud={portJson.baudLate}");
        serialPort = new SerialPort(portJson.com, portJson.baudLate, Parity.None, 8, StopBits.One);
        Debug.Log("포트연결시도");
        serialPort.Open();
        if (serialPort.IsOpen)
        {

            Debug.Log("연결완료");
            StartSerialPortReader();
        }
        SendData("H1");

        //serialPort.ReadTimeout = 500;

    }


    // 데이터 읽기
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            ReceivedData("M3");
        }
        //if (serialPort == null || !serialPort.IsOpen)
        //{
        //    if (!isReconnecting)   // 재연결 중복방지
        //        StartCoroutine(ReconnectRoutine());
        //}
    }
    async void StartSerialPortReader()
    {
        cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        //OnConnected?.Invoke();

        while (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                // 데이터를 수신

                string input = await Task.Run(() => ReadSerialData(), token);
                string data = input;

                if (!string.IsNullOrEmpty(input) && input.Length >= 2)
                {
                    Debug.Log("받은데이터 : " + input);
                    ReceivedData(input);
                }

            }
            catch (TimeoutException ex)
            {
                // 데이터가 없을 때는 무시
                // 데이터가 없을 때는 무시
                Debug.LogWarning("포트 읽기 오류 발생 (포트 끊김 가능): " + ex.Message);

                // 포트가 꺼졌다고 판단
                //HandleDisconnect();
            }

        }
        //HandleDisconnect();
    }
   
    private string ReadSerialData()
    {
        try
        {

            string input = serialPort.ReadExisting(); // 데이터 읽기
            Debug.Log($"필터 전 : {input}");
            if (!string.IsNullOrEmpty(input))
            {
                serialBuffer.Append(input); // (1)

                string processed = TryGetCompleteMessage(serialBuffer.ToString()); // (2)
                if (processed != null) // (3)
                {
                    Debug.Log("완전한 데이터 수신: " + processed); // (4)
                    serialBuffer.Clear(); // (5)
                }
                return processed;
            }
            return "";
            //return serialPort.ReadLine(); // 데이터 읽기
        }
        catch (TimeoutException)
        {

            return null;
        }
    }
    private string TryGetCompleteMessage(string buffer)
    {
        int newlineIndex = buffer.IndexOf('\r');
        //Debug.Log(newlineIndex);
        if (newlineIndex >= 0)
        {
           
            string complete = buffer.Substring(0, newlineIndex).Trim();
            return complete;
        }

        return null; // 아직 끝나지 않은 메시지
    }
  

    public void SendData(string message)
    {
        if (serialPort.IsOpen)
        {
            try
            {
                serialPort.WriteLine(message); // 메시지 송신 (줄 바꿈 추가)
                Debug.Log("Sent: " + message);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("송신 오류: " + ex.Message);
            }
        }
        else
        {
            Debug.LogWarning("포트가 열려 있지 않음 - 송신 실패");
        }
    }
    protected virtual void ReceivedData(string data)
    {
        //상속하고 받은데이터로 프로젝트에 맞는 기능 구현
        //Debug.Log($"{data} 신호보내기");

        sendMessage.text = $"{data} 수신완료";
        if (customSPManager != null)
        {
            customSPManager.lapseTimer = 0;
            customSPManager.isWaitMode = false;
            if (data[0] != 'S')
            {
                notePlayer.SetStrong(strongDatas[cashingStrong[1] - '1'].data);
            }
        }
        switch (data[0])
        {
            case 'M':
                notePlayer.MusicalInstrumentPreSet(data[1]);
                break;
            case 'B':
                Debug.Log($"BPM변경 {data[1]}번");
                if (data[1] =='1')
                {
                    notePlayer.SetBPM(120);
                }
                else if (data[1] == '2')
                {
                    notePlayer.SetBPM(160);
                }
                else if (data[1] == '3')
                {
                    notePlayer.SetBPM(200);
                }
                else if (data[1] == '4')
                {
                    notePlayer.SetBPM(240);
                }
                break;
            case 'S':
                Debug.Log($"셈여림변경 {data[1]}번");
                notePlayer.SetStrong(strongDatas[data[1]- '1'].data);
                cashingStrong = data;
                break;
            case 'T':
                Debug.Log($"박자변경 {data[1]}번");
                notePlayer.SetTemp(tempDatas[data[1]- '1'].data);
                break;
        }


    }
    public void ReceivedData_public(string data)
    {
        ReceivedData(data);
    }
    public void TempPreSet(string protocol)
    {
        switch (protocol)
        {
            case "T":
                ReceivedData(protocol + (tempoIndex+1));
                tempoIndex++;
                tempoIndex %= 3;
                break;
            case "S":
                ReceivedData(protocol + (strongIndex+1));
                strongIndex++;
                strongIndex %= 3;
                break;
            case "M":
                ReceivedData(protocol + (instrumentIndex+1));
                instrumentIndex++;
                instrumentIndex %= 3;
                break;
            case "B":
                ReceivedData(protocol + (bpmIndex+1));
                bpmIndex++;
                bpmIndex %= 4;
                break;
        }


      

    }

    void OnApplicationQuit()
    {
        // 포트 닫기

        // 종료 시 쓰레드 정리 및 포트 닫기

        EndPort();

    }
    private void EndPort()
    {
        if (cancellationTokenSource != null)
        {
            Debug.Log("Task 종료");
            cancellationTokenSource.Cancel(); // 작업 취소
        }
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }

    private StringBuilder receiveBuffer = new StringBuilder();
    public void OnSerialData(string dataChunk)
    {
        // 새로 들어온 조각을 버퍼에 추가
        receiveBuffer.Append(dataChunk);

        while (true)
        {
            int newlineIndex = receiveBuffer.ToString().IndexOf('\r');
            if (newlineIndex < 0)
                break; // 아직 완전한 메시지가 없음

            // 완전한 한 줄 추출
            string complete = receiveBuffer.ToString(0, newlineIndex).Trim();

            // 처리 (큐에 넣거나 이벤트로 넘김)
            ReceivedData(complete);

            // 버퍼에서 사용한 부분 제거
            receiveBuffer.Remove(0, newlineIndex + 1);
        }
    }

   
    private void TryOpenPort()
    {
        try
        {
            serialPort = new SerialPort(portJson.com, portJson.baudLate, Parity.None, 8, StopBits.One);
            serialPort.Open();

            if (serialPort.IsOpen)
            {
                Debug.Log("포트 연결 성공!");
                StartSerialPortReader();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"포트 열기 실패: {ex.Message}");
        }
    }
    private IEnumerator delay_co()
    {
        yield return new WaitForSeconds(1);
        ReceivedData("M2");
        ReceivedData("S2");
        ReceivedData("T2");
        ReceivedData("B2");
    }
}
