using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    /// <summary>
    ///     是否能旋转X轴
    /// </summary>
    public bool canRotation_X = true;

    /// <summary>
    ///     是否能旋转Y轴
    /// </summary>
    public bool canRotation_Y = true;

    /// <summary>
    ///     是否能缩放
    /// </summary>
    public bool canScale = true;

    #region 属性

    /// <summary>
    ///     相机看向的目标
    /// </summary>
    public Transform target;

    /// <summary>
    ///     鼠标按钮，指针和滚轮的设置
    /// </summary>
    public MouseSettings mouseSettings = new(0, 10, 10);

    /// <summary>
    ///     角度范围限制
    /// </summary>
    public Range angleRange = new(-90, 90);

    /// <summary>
    ///     距离极限范围
    /// </summary>
    public Range distanceRange = new(0, 10);

    /// <summary>
    ///     平面上的矩形区域
    /// </summary>
    public PlaneArea PlaneArea;

    /// <summary>
    ///     摄像机对准目标
    /// </summary>
    public AlignTarget AlignTarget;

    /// <summary>
    ///     阻尼器可移动和旋转的范围.
    /// </summary>
    [Range(0, 10)] public float damper = 5;

    /// <summary>
    ///     相机当前的角度.
    /// </summary>
    public Vector2 CurrentAngles { protected set; get; }

    /// <summary>
    ///     相机到目标的当前距离.
    /// </summary>
    public float CurrentDistance { protected set; get; }

    /// <summary>
    ///     相机目标角度.
    /// </summary>
    protected Vector2 targetAngles;

    /// <summary>
    ///     相机到目标的距离
    /// </summary>
    protected float targetDistance;

    #endregion

    #region Protected Method

    protected virtual void Start()
    {
        CurrentAngles = targetAngles = transform.eulerAngles;
        CurrentDistance = targetDistance = Vector3.Distance(transform.position, target.position);
    }

    protected virtual void LateUpdate()
    {
#if UNITY_EDITOR
        AroundByMouseInput();
#elif UNITY_STANDALONE_WIN
                AroundByMobileInput();
#elif UNITY_ANDROID || UNITY_IPHONE
                AroundByMobileInput();
#endif
    }

    //记录上一次手机触摸位置判断用户是在左放大还是缩小手势  
    private Vector2 oldPosition1;
    private Vector2 oldPosition2;

    //是否单指操作
    private bool m_IsSingleFinger;

    /// <summary>
    ///     移动端（Win平板）
    /// </summary>
    protected void AroundByMobileInput()
    {
        if (Input.touchCount == 1)
        {
            if (Input.touches[0].phase == TouchPhase.Moved)
            {
                //手机端可调用此代码（window电脑只能获取鼠标不能获取触摸板）
                targetAngles.y += Input.GetAxis("Mouse X") * mouseSettings.pointerSensitivity;
                targetAngles.x -= Input.GetAxis("Mouse Y") * mouseSettings.pointerSensitivity;

                //window电脑可获取（移动端也可以使用）
                targetAngles.y += Input.touches[0].deltaPosition.x * Time.deltaTime * 5;
                targetAngles.x -= Input.touches[0].deltaPosition.y * Time.deltaTime * 5;

                //范围
                targetAngles.x = Mathf.Clamp(targetAngles.x, angleRange.min, angleRange.max);
            }

            //鼠标指针
            m_IsSingleFinger = true;
        }

        //鼠标滚轮
        if (canScale)
            if (Input.touchCount > 1)
            {
                //计算出当前两点触摸点的位置  
                if (m_IsSingleFinger)
                {
                    oldPosition1 = Input.GetTouch(0).position;
                    oldPosition2 = Input.GetTouch(1).position;
                }

                if (Input.touches[0].phase == TouchPhase.Moved && Input.touches[1].phase == TouchPhase.Moved)
                {
                    var tempPosition1 = Input.GetTouch(0).position;
                    var tempPosition2 = Input.GetTouch(1).position;

                    var currentTouchDistance = Vector3.Distance(tempPosition1, tempPosition2);
                    var lastTouchDistance = Vector3.Distance(oldPosition1, oldPosition2);

                    //计算上次和这次双指触摸之间的距离差距  
                    //然后去更改摄像机的距离  
                    targetDistance -= (currentTouchDistance - lastTouchDistance) * Time.deltaTime *
                                      mouseSettings.wheelSensitivity;

                    //备份上一次触摸点的位置，用于对比  
                    oldPosition1 = tempPosition1;
                    oldPosition2 = tempPosition2;
                    m_IsSingleFinger = false;
                }
            }

        targetDistance = Mathf.Clamp(targetDistance, distanceRange.min, distanceRange.max);

        //缓动
        CurrentAngles = Vector2.Lerp(CurrentAngles, targetAngles, damper * Time.deltaTime);
        CurrentDistance = Mathf.Lerp(CurrentDistance, targetDistance, damper * Time.deltaTime);


        if (!canRotation_X) targetAngles.y = 0;
        if (!canRotation_Y) targetAngles.x = 0;

        //实时位置旋转
        transform.rotation = Quaternion.Euler(CurrentAngles);
        //实时位置移动
        transform.position = target.position - transform.forward * CurrentDistance;
    }

    /// <summary>
    ///     相机通过鼠标输入围绕目标
    /// </summary>
    protected void AroundByMouseInput()
    {
        if (Input.GetMouseButton(mouseSettings.mouseButtonID))
        {
            //鼠标指针
            targetAngles.y += Input.GetAxis("Mouse X") * mouseSettings.pointerSensitivity;
            targetAngles.x -= Input.GetAxis("Mouse Y") * mouseSettings.pointerSensitivity;

            //范围
            targetAngles.x = Mathf.Clamp(targetAngles.x, angleRange.min, angleRange.max);
        }

        //鼠标滚轮
        if (canScale) targetDistance -= Input.GetAxis("Mouse ScrollWheel") * mouseSettings.wheelSensitivity;
        targetDistance = Mathf.Clamp(targetDistance, distanceRange.min, distanceRange.max);

        //缓动
        CurrentAngles = Vector2.Lerp(CurrentAngles, targetAngles, damper * Time.deltaTime);
        CurrentDistance = Mathf.Lerp(CurrentDistance, targetDistance, damper * Time.deltaTime);


        if (!canRotation_X) targetAngles.y = 0;
        if (!canRotation_Y) targetAngles.x = 0;


        //实时位置旋转
        transform.rotation = Quaternion.Euler(CurrentAngles);
        //实时位置移动
        transform.position = target.position - transform.forward * CurrentDistance;
    }

    #endregion
}

[Serializable]
public struct MouseSettings
{
    /// <summary>
    ///     鼠标按键的ID
    /// </summary>
    public int mouseButtonID;

    /// <summary>
    ///     鼠标指针的灵敏度.
    /// </summary>
    public float pointerSensitivity;

    /// <summary>
    ///     鼠标滚轮的灵敏度
    /// </summary>
    public float wheelSensitivity;

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="mouseButtonID">鼠标按钮的ID</param>
    /// <param name="pointerSensitivity">鼠标指针的灵敏度</param>
    /// <param name="wheelSensitivity">鼠标滚轮的灵敏度</param>
    public MouseSettings(int mouseButtonID, float pointerSensitivity, float wheelSensitivity)
    {
        this.mouseButtonID = mouseButtonID;
        this.pointerSensitivity = pointerSensitivity;
        this.wheelSensitivity = wheelSensitivity;
    }
}

/// <summary>
///     范围从最小到最大
/// </summary>
[Serializable]
public struct Range
{
    /// <summary>
    ///     范围的最小值
    /// </summary>
    public float min;

    /// <summary>
    ///     范围的最大值
    /// </summary>
    public float max;

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="min">范围的最小值</param>
    /// <param name="max">范围的最大值</param>
    public Range(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
}

/// <summary>
///     平面上的矩形区域
/// </summary>
[Serializable]
public struct PlaneArea
{
    /// <summary>
    ///     区域中心
    /// </summary>
    public Transform center;

    /// <summary>
    ///     区域宽度
    /// </summary>
    public float width;

    /// <summary>
    ///     区域长度
    /// </summary>
    public float length;

    /// <summary>
    ///     平面区域_构造函数
    /// </summary>
    /// <param name="center">区域中心</param>
    /// <param name="width">区域宽度</param>
    /// <param name="length">区域长度</param>
    public PlaneArea(Transform center, float width, float length)
    {
        this.center = center;
        this.width = width;
        this.length = length;
    }
}

/// <summary>
///     摄像机对准目标
/// </summary>
[Serializable]
public struct AlignTarget
{
    /// <summary>
    ///     对准目标中心
    /// </summary>
    public Transform center;

    /// <summary>
    ///     对齐角度
    /// </summary>
    public Vector2 angles;

    /// <summary>
    ///     相机到目标中心的距离
    /// </summary>
    public float distance;

    /// <summary>
    ///     角度范围限制
    /// </summary>
    public Range angleRange;

    /// <summary>
    ///     距离范围限制
    /// </summary>
    public Range distanceRange;

    /// <summary>
    ///     对准目标_构造函数
    /// </summary>
    /// <param name="center">对准目标中心</param>
    /// <param name="angles">对齐角度</param>
    /// <param name="distance">相机到目标中心的距离</param>
    /// <param name="angleRange">角度范围限制</param>
    /// <param name="distanceRange">距离范围限制</param>
    public AlignTarget(Transform center, Vector2 angles, float distance, Range angleRange, Range distanceRange)
    {
        this.center = center;
        this.angles = angles;
        this.distance = distance;
        this.angleRange = angleRange;
        this.distanceRange = distanceRange;
    }
}