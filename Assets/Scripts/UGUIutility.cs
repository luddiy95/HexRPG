using UnityEngine;

public static class UGuiUtility
{
    /// <summary>
    /// �w��object ���߂�canvas �� RectTransform �𓾂�B
    /// </summary>
    /// <param name="gameObject"></param>
    public static Canvas NearestCanvas(this GameObject gameObject)
    {
        return SearchCanvas(gameObject.transform);

        Canvas SearchCanvas(Transform t)
        {
            if (t == null)
            {
                return null;
            }
            else if (t.TryGetComponent(out Canvas c) == true)
            {
                return c;
            }
            else
            {
                return SearchCanvas(t.parent);
            }
        }
    }

    /// <summary>
    /// world camera �� canvas (RectTransform)����AUI Canvas ��̈ʒu�𓾂�B
    /// </summary>
    /// <param name="mainCamera"></param>
    /// <param name="canvas"></param>
    /// <param name="worldPosition"></param>
    /// <returns></returns>
    public static Vector2 WorldToCanvasLocal(Camera mainCamera, Canvas canvas, Vector3 worldPosition)
    {
        var canvasTransform = (RectTransform)canvas.transform;
        if (mainCamera == null)
        {
            return Vector2.zero;
        }
        Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPosition);
        var localPos = Vector2.zero;
        switch (canvas.renderMode)
        {
            case RenderMode.ScreenSpaceOverlay:
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTransform, screenPos, null, out localPos);
                break;
            case RenderMode.ScreenSpaceCamera:
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTransform, screenPos, canvas.worldCamera, out localPos);
                break;
            default:
                break;
        }
        return localPos;
    }
}