using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;

namespace CM3D2CameraUtility
{
    [PluginFilter("CM3D2x64"),
    PluginFilter("CM3D2x86"),
    PluginFilter("CM3D2VRx64"),
    PluginName("Camera Utility"),
    PluginVersion("2.0.1.2")]

    public class CameraUtility : PluginBase
    {
        //移動関係キー設定
        private KeyCode bgLeftMoveKey = KeyCode.LeftArrow;
        private KeyCode bgRightMoveKey = KeyCode.RightArrow;
        private KeyCode bgForwardMoveKey = KeyCode.UpArrow;
        private KeyCode bgBackMoveKey = KeyCode.DownArrow;
        private KeyCode bgUpMoveKey = KeyCode.PageUp;
        private KeyCode bgDownMoveKey = KeyCode.PageDown;
        private KeyCode bgLeftRotateKey = KeyCode.Delete;
        private KeyCode bgRightRotateKey = KeyCode.End;
        private KeyCode bgLeftPitchKey = KeyCode.Insert;
        private KeyCode bgRightPitchKey = KeyCode.Home;
        private KeyCode bgInitializeKey = KeyCode.Backspace;

        //VR用移動関係キー設定
        private KeyCode bgLeftMoveKeyVR = KeyCode.J;
        private KeyCode bgRightMoveKeyVR = KeyCode.L;
        private KeyCode bgForwardMoveKeyVR = KeyCode.I;
        private KeyCode bgBackMoveKeyVR = KeyCode.K;
        private KeyCode bgUpMoveKeyVR = KeyCode.Alpha0;
        private KeyCode bgDownMoveKeyVR = KeyCode.P;
        private KeyCode bgLeftRotateKeyVR = KeyCode.U;
        private KeyCode bgRightRotateKeyVR = KeyCode.O;
        private KeyCode bgLeftPitchKeyVR = KeyCode.Alpha8;
        private KeyCode bgRightPitchKeyVR = KeyCode.Alpha9;
        private KeyCode bgInitializeKeyVR = KeyCode.Backspace;

        //カメラ操作関係キー設定
        private KeyCode cameraLeftPitchKey = KeyCode.Period;
        private KeyCode cameraRightPitchKey = KeyCode.Backslash;
        private KeyCode cameraPitchInitializeKey = KeyCode.Slash;
        private KeyCode cameraFoVPlusKey = KeyCode.RightBracket;
        //Equalsになっているが日本語キーボードだとセミコロン
        private KeyCode cameraFoVMinusKey = KeyCode.Equals;
        //Semicolonになっているが日本語キーボードだとコロン
        private KeyCode cameraFoVInitializeKey = KeyCode.Semicolon;

        //こっち見てキー設定
        private KeyCode eyetoCamToggleKey = KeyCode.G;
        private KeyCode eyetoCamChangeKey = KeyCode.T;

        //夜伽UI消しキー設定
        private KeyCode hideUIToggleKey = KeyCode.Tab;

        //FPSモード切替キー設定
        private KeyCode cameraFPSModeToggleKey = KeyCode.F;

        //TimeScale変更関係キー設定
        private KeyCode timeScalePlusKey = KeyCode.LeftBracket;
        private KeyCode timeScaleMinusKey = KeyCode.P;
        private KeyCode timeScaleInitialize = KeyCode.BackQuote;
        private KeyCode timeScaleZero = KeyCode.O;

        private enum modKey
        {
            Shift,
            Alt,
            Ctrl
        }

        private Maid maid;
        private CameraMain mainCamera;
        private Transform mainCameraTransform;
        private Transform maidTransform;
        private Transform bg;
        private GameObject manHead;
        private GameObject uiObject;

        private float defaultFOV = 35f;
        private bool allowUpdate = false;
        private bool occulusVR = false;
        private bool fpsMode = false;
        private bool eyetoCamToggle = false;

        private float cameraRotateSpeed = 1f;
        private float cameraFOVChangeSpeed = 0.25f;
        private float floorMoveSpeed = 0.05f;
        private float maidRotateSpeed = 2f;
        private float fpsModeFoV = 60f;

        private int sceneLevel;
        private int frameCount = 0;

        private float fpsOffsetForward = 0.02f;
        private float fpsOffsetUp = -0.06f;
        private float fpsOffsetRight = 0f;

        ////以下の数値だと男の目の付近にカメラが移動しますが
        ////うちのメイドはデフォで顔ではなく喉元見てるのであんまりこっち見てくれません
        //private float fpsOffsetForward = 0.1f;
        //private float fpsOffsetUp = 0.12f;

        private Vector3 oldPos;
        private Vector3 oldTargetPos;
        private float oldDistance;
        private float oldFoV;
        private Quaternion oldRotation;

        private bool oldEyetoCamToggle;
        private int eyeToCamIndex = 0;

        private bool uiVisible = true;
        private GameObject profilePanel;

        public void Awake()
        {
            GameObject.DontDestroyOnLoad(this);

            string path = Application.dataPath;
            occulusVR = path.Contains("CM3D2VRx64");
            if (occulusVR)
            {
                bgLeftMoveKey = bgLeftMoveKeyVR;
                bgRightMoveKey = bgRightMoveKeyVR;
                bgForwardMoveKey = bgForwardMoveKeyVR;
                bgBackMoveKey = bgBackMoveKeyVR;
                bgUpMoveKey = bgUpMoveKeyVR;
                bgDownMoveKey = bgDownMoveKeyVR;
                bgLeftRotateKey = bgLeftRotateKeyVR;
                bgRightRotateKey = bgRightRotateKeyVR;
                bgLeftPitchKey = bgLeftPitchKeyVR;
                bgRightPitchKey = bgRightPitchKeyVR;
                bgInitializeKey = bgInitializeKeyVR;
            }
        }

        public void Start()
        {
            mainCameraTransform = Camera.main.gameObject.transform;
        }

        public void OnLevelWasLoaded(int level)
        {
            sceneLevel = level;

            maid = GameMain.Instance.CharacterMgr.GetMaid(0);

            if (maid)
            {
                maidTransform = maid.body0.transform;
            }

            bg = GameObject.Find("__GameMain__/BG").transform;

            mainCamera = GameMain.Instance.MainCamera;

            if (maid && bg && maidTransform)
            {
                allowUpdate = true;
            }
            else
            {
                allowUpdate = false;
            }

            if (occulusVR)
            {
                uiObject = GameObject.Find("ovr_screen");
            }
            else
            {
                uiObject = GameObject.Find("/UI Root/Camera");
                defaultFOV = Camera.main.fieldOfView;
            }

            if (level == 5)
            {
                GameObject uiRoot = GameObject.Find("/UI Root");
                profilePanel = uiRoot.transform.Find("ProfilePanel").gameObject;
            }
            else if (level == 12)
            {
                GameObject uiRoot = GameObject.Find("/UI Root");
                profilePanel = uiRoot.transform.Find("UserEditPanel").gameObject;
            }
            fpsMode = false;
        }

        private bool getModKeyPressing(modKey key)
        {
            switch (key)
            {
                case modKey.Shift:
                    return (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                case modKey.Alt:
                    return (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
                case modKey.Ctrl:
                    return (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
                default:
                    return false;
            }
        }

        private void SaveCameraPos()
        {
            oldPos = mainCamera.GetPos();
            oldTargetPos = mainCamera.GetTargetPos();
            oldDistance = mainCamera.GetDistance();
            oldRotation = mainCameraTransform.rotation;
            oldFoV = Camera.main.fieldOfView;
        }

        private void LoadCameraPos()
        {
            mainCameraTransform.rotation = oldRotation;
            mainCamera.SetPos(oldPos);
            mainCamera.SetTargetPos(oldTargetPos, true);
            mainCamera.SetDistance(oldDistance, true);
            Camera.main.fieldOfView = oldFoV;
        }

        private Vector3 GetYotogiPlayPosition()
        {
            var field = mainCamera.GetType().GetField("m_vCenter", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            return (Vector3)field.GetValue(mainCamera);
        }

        private void FirstPersonCamera()
        {
            if (sceneLevel == 14)
            {
                if (!manHead)
                {
                    if (frameCount == 60)
                    {
                        GameObject manBipHead = GameObject.Find("__GameMain__/Character/Active/AllOffset/Man[0]/Offset/_BO_mbody/ManBip/ManBip Spine/ManBip Spine1/ManBip Spine2/ManBip Neck/ManBip Head");

                        if (manBipHead)
                        {
                            Transform[] manHedas = manBipHead.GetComponentsInChildren<Transform>();


                            foreach (Transform mh in manHedas)
                            {
                                if (mh.name.IndexOf("_SM_mhead") > -1)
                                {
                                    GameObject smManHead = mh.gameObject;
                                    foreach (Transform smmh in smManHead.transform)
                                    {
                                        if (smmh.name.IndexOf("ManHead") > -1)
                                        {
                                            manHead = smmh.gameObject;
                                        }
                                    }
                                }
                            }
                        }
                        frameCount = 0;
                    }
                    else
                    {
                        frameCount++;
                    }
                }
                else
                {
                    if (occulusVR)
                    {
                        if (Input.GetKeyDown(cameraFPSModeToggleKey))
                        {
                            //    eyetoCamToggle = false;
                            //    maid.EyeToCamera(Maid.EyeMoveType.無し, 0f);
                            Vector3 localPos = uiObject.transform.localPosition;
                            mainCamera.SetPos(manHead.transform.position);
                            uiObject.transform.position = manHead.transform.position;
                            uiObject.transform.localPosition = localPos;

                        }
                    }
                    else
                    {
                        if (Input.GetKeyDown(cameraFPSModeToggleKey))
                        {
                            fpsMode = !fpsMode;
                            Console.WriteLine("fpsmode = " + fpsMode.ToString());
                            if (fpsMode)
                            {
                                SaveCameraPos();

                                Camera.main.fieldOfView = fpsModeFoV;
                                eyetoCamToggle = false;
                                maid.EyeToCamera(Maid.EyeMoveType.無し, 0f);

                                mainCameraTransform.rotation = Quaternion.LookRotation(-manHead.transform.up);

                                manHead.renderer.enabled = false;
                            }
                            else
                            {
                                Vector3 cameraTargetPosFromScript = GetYotogiPlayPosition();

                                if (oldTargetPos != cameraTargetPosFromScript)
                                {
                                    Console.WriteLine("Position Changed!");
                                    oldTargetPos = cameraTargetPosFromScript;
                                }
                                manHead.renderer.enabled = true;

                                LoadCameraPos();
                                //Camera.main.fieldOfView = defaultFOV;

                                //mainCamera.ResetFromScriptOnTarget();
                                eyetoCamToggle = oldEyetoCamToggle;
                                oldEyetoCamToggle = eyetoCamToggle;
                            }
                        }
                        if (fpsMode)
                        {
                            Vector3 cameraTargetPosFromScript = GetYotogiPlayPosition();
                            if (oldTargetPos != cameraTargetPosFromScript)
                            {
                                Console.WriteLine("Position Changed!");
                                mainCameraTransform.rotation = Quaternion.LookRotation(-manHead.transform.up);
                                oldTargetPos = cameraTargetPosFromScript;

                            }

                            mainCamera.SetPos(manHead.transform.position + manHead.transform.up * fpsOffsetUp + manHead.transform.right * fpsOffsetRight + manHead.transform.forward * fpsOffsetForward);
                            mainCamera.SetTargetPos(manHead.transform.position + manHead.transform.up * fpsOffsetUp + manHead.transform.right * fpsOffsetRight + manHead.transform.forward * fpsOffsetForward, true);
                            mainCamera.SetDistance(0f, true);
                        }
                    }
                }
            }
        }

        private void ExtendedCameraHandle()
        {
            if (!occulusVR)
            {
                if (mainCameraTransform)
                {
                    if (Input.GetKey(cameraFoVMinusKey))
                    {
                        Camera.main.fieldOfView += -cameraFOVChangeSpeed;
                    }
                    if (Input.GetKey(cameraFoVInitializeKey))
                    {
                        Camera.main.fieldOfView = defaultFOV;
                    }
                    if (Input.GetKey(cameraFoVPlusKey))
                    {
                        Camera.main.fieldOfView += cameraFOVChangeSpeed;
                    }
                    if (Input.GetKey(cameraLeftPitchKey))
                    {
                        mainCameraTransform.Rotate(0, 0, cameraRotateSpeed);
                    }
                    if (Input.GetKey(cameraPitchInitializeKey))
                    {
                        mainCameraTransform.eulerAngles = new Vector3(
                            mainCameraTransform.rotation.eulerAngles.x,
                            mainCameraTransform.rotation.eulerAngles.y,
                            0f);
                    }
                    if (Input.GetKey(cameraRightPitchKey))
                    {
                        mainCameraTransform.Rotate(0, 0, -cameraRotateSpeed);
                    }
                }
            }
        }

        private void FloorMover(float moveSpeed, float rotateSpeed)
        {
            if (bg)
            {
                Vector3 cameraForward = mainCameraTransform.TransformDirection(Vector3.forward);
                Vector3 cameraRight = mainCameraTransform.TransformDirection(Vector3.right);
                Vector3 cameraUp = mainCameraTransform.TransformDirection(Vector3.up);

                Vector3 direction = Vector3.zero;

                if (Input.GetKey(bgLeftMoveKey))
                {
                    direction += new Vector3(cameraRight.x, 0f, cameraRight.z) * moveSpeed;
                }
                if (Input.GetKey(bgRightMoveKey))
                {
                    direction += new Vector3(cameraRight.x, 0f, cameraRight.z) * -moveSpeed;
                }
                if (Input.GetKey(bgBackMoveKey))
                {
                    direction += new Vector3(cameraForward.x, 0f, cameraForward.z) * moveSpeed;
                }
                if (Input.GetKey(bgForwardMoveKey))
                {
                    direction += new Vector3(cameraForward.x, 0f, cameraForward.z) * -moveSpeed;
                }
                if (Input.GetKey(bgUpMoveKey))
                {
                    direction += new Vector3(0f, cameraUp.y, 0f) * -moveSpeed;
                }
                if (Input.GetKey(bgDownMoveKey))
                {
                    direction += new Vector3(0f, cameraUp.y, 0f) * moveSpeed;
                }

                bg.localPosition += direction;

                if (Input.GetKey(bgLeftRotateKey))
                {
                    bg.RotateAround(maidTransform.transform.position, Vector3.up, rotateSpeed);
                }
                if (Input.GetKey(bgRightRotateKey))
                {
                    bg.RotateAround(maidTransform.transform.position, Vector3.up, -rotateSpeed);
                }
                if (Input.GetKey(bgLeftPitchKey))
                {
                    bg.RotateAround(maidTransform.transform.position, new Vector3(cameraForward.x, 0f, cameraForward.z), rotateSpeed);
                }
                if (Input.GetKey(bgRightPitchKey))
                {
                    bg.RotateAround(maidTransform.transform.position, new Vector3(cameraForward.x, 0f, cameraForward.z), -rotateSpeed);
                }

                if (getModKeyPressing(modKey.Alt) && (Input.GetKey(bgLeftRotateKey) || Input.GetKey(bgRightRotateKey)))
                {
                    bg.RotateAround(maidTransform.position, Vector3.up, -bg.rotation.eulerAngles.y);
                }
                if (getModKeyPressing(modKey.Alt) && (Input.GetKey(bgLeftPitchKey) || Input.GetKey(bgRightPitchKey)))
                {
                    bg.RotateAround(maidTransform.position, Vector3.forward, -bg.rotation.eulerAngles.z);
                    bg.RotateAround(maidTransform.position, Vector3.right, -bg.rotation.eulerAngles.x);
                }
                if (getModKeyPressing(modKey.Alt) && (Input.GetKey(bgLeftMoveKey) || Input.GetKey(bgRightMoveKey) || Input.GetKey(bgBackMoveKey) || Input.GetKey(bgForwardMoveKey)))
                {
                    bg.localPosition = new Vector3(0f, bg.localPosition.y, 0f);
                }
                if (getModKeyPressing(modKey.Alt) && (Input.GetKey(bgUpMoveKey) || Input.GetKey(bgDownMoveKey)))
                {
                    bg.localPosition = new Vector3(bg.localPosition.x, 0f, bg.localPosition.z);
                }
                if (Input.GetKeyDown(bgInitializeKey))
                {
                    bg.localPosition = Vector3.zero;
                    bg.RotateAround(maidTransform.position, Vector3.up, -bg.rotation.eulerAngles.y);
                    bg.RotateAround(maidTransform.position, Vector3.right, -bg.rotation.eulerAngles.x);
                    bg.RotateAround(maidTransform.position, Vector3.forward, -bg.rotation.eulerAngles.z);
                    bg.RotateAround(maidTransform.position, Vector3.up, -bg.rotation.eulerAngles.y);
                }
            }
        }

        private void LookAtThis()
        {
            if (Input.GetKeyDown(eyetoCamChangeKey))
            {
                if (eyeToCamIndex == Enum.GetNames(typeof(Maid.EyeMoveType)).Length - 1)
                {
                    eyetoCamToggle = false;
                    eyeToCamIndex = 0;
                }
                else
                {
                    eyeToCamIndex++;
                    eyetoCamToggle = true;
                }
                maid.EyeToCamera((Maid.EyeMoveType)eyeToCamIndex, 0f);
                Console.WriteLine("EyeToCam:{0}", eyeToCamIndex);
            }

            if (Input.GetKeyDown(eyetoCamToggleKey))
            {
                eyetoCamToggle = !eyetoCamToggle;
                //Console.WriteLine("Eye to Cam : {0}", eyetoCamToggle);
                if (!eyetoCamToggle)
                {
                    maid.EyeToCamera(Maid.EyeMoveType.無し, 0f);
                    eyeToCamIndex = 0;
                    Console.WriteLine("EyeToCam:{0}", eyeToCamIndex);
                }
                else
                {
                    maid.EyeToCamera(Maid.EyeMoveType.目と顔を向ける, 0f);
                    eyeToCamIndex = 5;
                    Console.WriteLine("EyeToCam:{0}", eyeToCamIndex);
                }
            }
        }

        private void TimeScaleChanger()
        {
            if (Input.GetKeyDown(timeScaleMinusKey))
            {
                Time.timeScale = Mathf.Max(0f, Time.timeScale - 0.2f);
                Console.WriteLine("TileScale:{0}", Time.timeScale);
            }
            if (Input.GetKeyDown(timeScalePlusKey))
            {
                Time.timeScale += 0.2f;
                Console.WriteLine("TileScale:{0}", Time.timeScale);
            }
            if (Input.GetKeyDown(timeScaleZero))
            {
                Time.timeScale = 0f;
                Console.WriteLine("TileScale:{0}", Time.timeScale);
            }
            if (Input.GetKeyDown(timeScaleInitialize))
            {
                Time.timeScale = 1f;
                Console.WriteLine("TileScale:{0}", Time.timeScale);
            }
        }

        private void HideUI()
        {
            if (Input.GetKeyDown(hideUIToggleKey))
            {
                if (sceneLevel == 5 || sceneLevel == 14)
                {
                    var field = GameMain.Instance.MainCamera.GetType().GetField("m_eFadeState", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);

                    int i = (int)field.GetValue(mainCamera);
                    //Console.WriteLine("FadeState:{0}", i);
                    if (i == 0)
                    {
                        uiVisible = !uiVisible;
                        if (uiObject)
                        {
                            uiObject.SetActive(uiVisible);
                        }
                    }
                    Console.WriteLine("UIVisible:{0}", uiVisible);
                }
            }
        }

        public void Update()
        {

            if (sceneLevel == 5)
            {
                if (profilePanel.activeSelf)
                {
                    allowUpdate = false;
                }
                else
                {
                    allowUpdate = true;
                }
            }
            else if (sceneLevel == 12)
            {
                if (profilePanel.activeSelf)
                {
                    allowUpdate = false;
                }
                else
                {
                    allowUpdate = true;
                }
            }

            if (allowUpdate)
            {
                float moveSpeed = floorMoveSpeed;
                float rotateSpeed = maidRotateSpeed;

                if (getModKeyPressing(modKey.Shift))
                {
                    moveSpeed *= 0.1f;
                    rotateSpeed *= 0.1f;
                }

                //TimeScaleChanger();

                FirstPersonCamera();

                LookAtThis();

                FloorMover(moveSpeed, rotateSpeed);

                if (!occulusVR)
                {
                    ExtendedCameraHandle();

                    HideUI();
                }
            }
        }
    }
}
