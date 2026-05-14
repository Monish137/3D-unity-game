using System.Collections;
using UnityEngine;

namespace UnityStandardAssets.CrossPlatformInput
{
    public static class CrossPlatformInputManager
    {
        public static float GetAxis(string axisName)
        {
            return Input.GetAxis(axisName);
        }

        public static bool GetButtonDown(string buttonName)
        {
            return Input.GetButtonDown(buttonName);
        }
    }
}

namespace UnityStandardAssets.Utility
{
    [System.Serializable]
    public class MouseLook
    {
        public float XSensitivity = 2f;
        public float YSensitivity = 2f;

        public void Init(Transform character, Transform camera)
        {
        }

        public void LookRotation(Transform character, Transform camera)
        {
            if (character == null || camera == null)
            {
                return;
            }

            float yRotation = Input.GetAxis("Mouse X") * XSensitivity;
            float xRotation = Input.GetAxis("Mouse Y") * YSensitivity;

            character.Rotate(0f, yRotation, 0f);
            camera.Rotate(-xRotation, 0f, 0f);
        }

        public void UpdateCursorLock()
        {
        }
    }

    [System.Serializable]
    public class FOVKick
    {
        private Camera camera;
        private float originalFov;

        public void Setup(Camera targetCamera)
        {
            camera = targetCamera;
            if (camera != null)
            {
                originalFov = camera.fieldOfView;
            }
        }

        public IEnumerator FOVKickUp()
        {
            if (camera != null)
            {
                camera.fieldOfView = originalFov + 5f;
            }

            yield return null;
        }

        public IEnumerator FOVKickDown()
        {
            if (camera != null)
            {
                camera.fieldOfView = originalFov;
            }

            yield return null;
        }
    }

    [System.Serializable]
    public class CurveControlledBob
    {
        private Vector3 originalCameraPosition;

        public void Setup(Camera targetCamera, float stepInterval)
        {
            if (targetCamera != null)
            {
                originalCameraPosition = targetCamera.transform.localPosition;
            }
        }

        public Vector3 DoHeadBob(float speed)
        {
            return originalCameraPosition;
        }
    }

    [System.Serializable]
    public class LerpControlledBob
    {
        public IEnumerator DoBobCycle()
        {
            yield return null;
        }

        public float Offset()
        {
            return 0f;
        }
    }
}
