using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using CodeMonkey.Utils;

public class controlObject : MonoBehaviour
{
    Camera cam;
    float mouseSensitivity = 23;
    RectTransform aim;
    Vector3 prevVelocity;
    float attackAngle;

    float aimDistist = 10000;
    Vector3 aimInWorldSpacPosition;
    Vector3 directionCircleInWorldWorldPosition;

    bool moveLeft;
    bool moveRight;
    bool moveUp;
    bool moveDown;
    bool leftRoll;
    bool rightRoll;

    float timePressLeft = 0;
    float timePressRight = 0;
    float timePressUp = 0;
    float timePressDown = 0;


    // public
    public float distForAddControlPlaneForce;
    public float distForAddAileronForce;
    public Vector3 tensor1;
    public Rigidbody rb;
    public Bullet2 bullet;
    public Vector3 angularVelocity;
    public Vector3 actualAcceleration;

    public Camera UICam;

    public Vector3 velocity;
    public float velocityMaggnitude;

    [Range(0.0f, 100f)] public float power = 0;
    public float engineThrust = 0;

    public float xAngleBetweenAinAndDirectionCircle;
    public float yAngleBetweenAinAndDirectionCircle;

    public RectTransform rectTransformDirectionCircle;
    public RectTransform rectTransformDirectionCircleArrow;

    public Text speedTextLabel;
    public Text powerTextLabel;
    public Text accelerationTextLabel;
    public Text attackAngleTextLabel;

    public Vector3 left_right_angle;
    public Vector3 left_right_attack_angle;
    public float left_right_attack_angle_magnitude;
    public Vector3 up_down_angle;
    public Vector3 up_down_attack_angle;
    public float up_down_attack_angle_magnitude;

    public GameObject leftRight;
    public GameObject upDown;
    public GameObject wing;
    public GameObject leftAileron;
    public GameObject rightAileron;

    public float upDownAngleCoeff = 1f;
    public float leftRightAngleCoeff = 1f;

    public float engineThrustCoeff;
    public float engineThrustKG;
    public float controlPlaneDragCoeff;
    public float upDownDragCoeff;
    public float leftRightDragCoeff;
    public float forwardDragCoeff;
    public float aileronsDragCoeff;
    public float liftingForceCoeff;
    public float upDownWingDragCoeff;
    public float leftRightWingDragCoeff;
    public float forwardWingDragCoeff;
    public float angularDragCoeff;
    public float controlPlaneDragEngineWindCoeff;

    void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;

        aim = GameObject.Find("direction_where_look_control_object").GetComponent<RectTransform>();

        rectTransformDirectionCircle.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);

        rb = GetComponent<Rigidbody>();
        rb.velocity = new Vector3(0, 0, 155);
        rb.maxAngularVelocity = 20;
        rb.inertiaTensor = tensor1 * rb.mass;

        directionCircleInWorldWorldPosition = transform.position + transform.forward * aimDistist;

        Cursor.visible = false;
    }

    void FixedUpdate()
    {
        rb.inertiaTensor = tensor1 * rb.mass;

        handleKeyInput();

        velocity = rb.velocity;
        velocityMaggnitude = rb.velocity.magnitude;

        if (moveLeft || moveRight || moveDown || moveUp)
            rotateControlPlaneByKey();
        else
            rotateControlPlaneByMouse();

        addEngineForce();
        addCorpusForces();
        addControlPlaneForces();
        addEleuronForces();
        addWingForces();
        addControlPlaneForcesWithEngineWind();

        calcAcceleration();
        calcAttackAngles();

        shoot();

        angularVelocity = rb.angularVelocity;
        rb.angularDrag = angularDragCoeff * rb.velocity.magnitude + 1f;
    }

    void Update()
    {
        //drawControlPlaneForces();
        //drawEleuronForces();
        //drawCorpusForces();
        //drawWingForces();
    }

    void LateUpdate() // late updat - not late fixed update
    {
        setAimAndDirectionCirclePosition();

        if (speedTextLabel)
            speedTextLabel.text = "velocity: " + (velocity.magnitude * 3.6).ToString("0.0") + " km/h";

        if (powerTextLabel)
            powerTextLabel.text = "power: " + power.ToString("0");

        if (accelerationTextLabel)
            accelerationTextLabel.text = "acceleration: (G): " + (actualAcceleration.magnitude / Physics.gravity.magnitude).ToString("0.0");

        if (attackAngleTextLabel)
            attackAngleTextLabel.text = "attack angle: " + attackAngle.ToString("0.0");

    }

    void OnDrawGizmos() /*OnDrawGizmosSelected  DrawLine*/
    {
        renderTransformGizmos();
        renderVelocityGizmos();

        //Gizmos.DrawRay(transform.position - transform.forward * distForAddControlPlaneForce, transform.up);
    }

    void setAimAndDirectionCirclePosition()
    {
        Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X") * mouseSensitivity, Input.GetAxis("Mouse Y") * mouseSensitivity);
        if (mouseDelta.x != 0 || mouseDelta.y != 0)
        {
            directionCircleInWorldWorldPosition = cam.ScreenToWorldPoint(new Vector3(rectTransformDirectionCircle.position.x + mouseDelta.x, rectTransformDirectionCircle.position.y + mouseDelta.y, aimDistist));
        }

        Vector3 directionCirclePosition = cam.WorldToScreenPoint(directionCircleInWorldWorldPosition);
        directionCirclePosition.z = 0;
        rectTransformDirectionCircle.position = directionCirclePosition;

        aimInWorldSpacPosition = transform.position + transform.forward * aimDistist;
        Vector3 aimPosition = cam.WorldToScreenPoint(aimInWorldSpacPosition);
        aimPosition.z = 0;
        aim.position = aimPosition;

        drawArrowIfObjectOutsideScreens(directionCircleInWorldWorldPosition, rectTransformDirectionCircleArrow);
    }

    void drawArrowIfObjectOutsideScreens(Vector3 targetPosition, RectTransform pointerRectTransform)
    {
        Vector3 fromPosition = /*transform.position;*/ cam.transform.position;
        fromPosition.z = 0;
        Vector3 dir = (targetPosition - fromPosition).normalized;
        float angle = UtilsClass.GetAngleFromVectorFloat(dir);
        pointerRectTransform.localEulerAngles = new Vector3(0, 0, angle);

        float borderSize = 50f;
        Vector3 tragetPositionScreenPoint = cam.WorldToScreenPoint(targetPosition);
        bool isOffsetScreen = tragetPositionScreenPoint.x <= borderSize || tragetPositionScreenPoint.x >= Screen.width - borderSize || tragetPositionScreenPoint.y <= borderSize || tragetPositionScreenPoint.y >= Screen.height - borderSize;

        if (isOffsetScreen)
        {
            Vector3 cappedTargetScreenPosition = tragetPositionScreenPoint;
            if (cappedTargetScreenPosition.x <= borderSize) cappedTargetScreenPosition.x = borderSize;
            if (cappedTargetScreenPosition.x >= Screen.width - borderSize) cappedTargetScreenPosition.x = Screen.width - borderSize;
            if (cappedTargetScreenPosition.y <= borderSize) cappedTargetScreenPosition.y = borderSize;
            if (cappedTargetScreenPosition.y >= Screen.height - borderSize) cappedTargetScreenPosition.y = Screen.height - borderSize;

            Vector3 pointerWorldPosition = UICam.ScreenToWorldPoint(cappedTargetScreenPosition);
            pointerRectTransform.position = pointerWorldPosition;
            pointerRectTransform.localPosition = new Vector3(pointerRectTransform.localPosition.x, pointerRectTransform.localPosition.y, 0f);
        }
        else
        {
            pointerRectTransform.position = new Vector3(3000, 0, 0);
            pointerRectTransform.localPosition = new Vector3(3000, 0, 0);
        }   
    }

    void shoot()
    {
        if (Input.GetMouseButton(0)) // GetMouseButton GetMouseButtonDown
        {
            Bullet2 bulletClone = Instantiate(bullet, new Vector3(transform.position.x, transform.position.y, transform.position.z) + transform.forward * 15, transform.rotation);
            bulletClone.speed = bulletClone.initBulletSpeed * new Vector3(transform.forward.x, transform.forward.y, transform.forward.z) + rb.velocity;
        }
    }

    void calcAttackAngles()
    {
        attackAngle = Vector3.Angle(transform.forward, rb.velocity);

        left_right_attack_angle = Vector3.ProjectOnPlane(rb.velocity, leftRight.transform.forward);
        left_right_attack_angle_magnitude = left_right_attack_angle.magnitude;

        up_down_attack_angle = Vector3.ProjectOnPlane(rb.velocity, upDown.transform.forward);
        up_down_attack_angle_magnitude = up_down_attack_angle.magnitude;
    }

    void renderTransformGizmos()
    {
        float gizmosSize = 5;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * gizmosSize * 1000);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * gizmosSize);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * gizmosSize);
    }

    void renderVelocityGizmos()
    {
        float gizmosSize = 1;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position /*- transform.forward * distForAddControlPlaneForce*/, velocity * gizmosSize);

        //Y UP
        //X RIGHT
        //Z FORWARD

        //Gizmos.color = Color.blue;
        //Gizmos.DrawRay(transform.position, new Vector3(velocity.x * gizmosSize, 0, 0)); // not correct ?

        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(transform.position, new Vector3(0, velocity.y * gizmosSize, 0)); // not correct ?

        //Gizmos.color = Color.green;
        //Gizmos.DrawRay(transform.position, new Vector3(0, 0, velocity.z * gizmosSize)); // not correct ?
    }

    void handleKeyInput()
    {
        moveLeft = false;
        moveRight = false;
        moveUp = false;
        moveDown = false;
        leftRoll = false;
        rightRoll = false;

        if (Input.GetMouseButton(1) && !Input.GetKey(KeyCode.Space)) return;

        if (Input.GetKey(KeyCode.Q))
        {
            moveLeft = true;
            timePressLeft += Time.fixedDeltaTime;
        }
        else
            timePressLeft = 0;

        if (Input.GetKey(KeyCode.E))
        {
            moveRight = true;
            timePressRight += Time.fixedDeltaTime;
        }
        else
            timePressRight = 0;

        if (Input.GetKey(KeyCode.W))
        {
            moveDown = true;
            timePressDown += Time.fixedDeltaTime;
        }
        else
            timePressDown = 0;

        if (Input.GetKey(KeyCode.S))
        {
            moveUp = true;
            timePressUp += Time.fixedDeltaTime;
        }
        else
            timePressUp = 0;

        if (Input.GetKey(KeyCode.A))
            leftRoll = true;

        if (Input.GetKey(KeyCode.D))
            rightRoll = true;

        if (Input.GetKey(KeyCode.LeftShift) && power < 100)
            power += 2;
        if (Input.GetKey(KeyCode.LeftControl) && power > 0)
            power -= 2;

        if (power > 100) power = 100;
        if (power < 0) power = 0;
    }

    void rotateControlPlaneByKey() // not korrect rotate doter planes ? - add empty game object
    {
        float maxAngle = 45;
        float maxRotationSpeed = 4f;

        float rotationSpeedCoeff = 3;

        float rotationSpeedRightCoeff = Mathf.Clamp01(rotationSpeedCoeff * timePressRight);
        float rotationSpeedLeftCoeff = Mathf.Clamp01(rotationSpeedCoeff * timePressLeft);
        float rotationSpeedUpCoeff = Mathf.Clamp01(rotationSpeedCoeff * timePressUp);
        float rotationSpeedDownCoeff = Mathf.Clamp01(rotationSpeedCoeff * timePressDown);

        if (moveRight)
        {
            leftRight.transform.Rotate(Vector3.up, -maxRotationSpeed * rotationSpeedRightCoeff);
            if (Vector3.Angle(transform.forward, leftRight.transform.forward) > maxAngle)
                leftRight.transform.localRotation = Quaternion.Euler(0, -maxAngle, 0);
        }
        if (moveLeft)
        {
            leftRight.transform.Rotate(Vector3.up, maxRotationSpeed * rotationSpeedLeftCoeff);
            if (Vector3.Angle(transform.forward, leftRight.transform.forward) > maxAngle)
                leftRight.transform.localRotation = Quaternion.Euler(0, maxAngle, 0);
        }
        if (moveUp)
        {
            upDown.transform.Rotate(Vector3.right, maxRotationSpeed * rotationSpeedUpCoeff);
            if (Vector3.Angle(transform.forward, upDown.transform.forward) > maxAngle)
                upDown.transform.localRotation = Quaternion.Euler(maxAngle, 0, 0);
        }
        if (moveDown)
        {
            upDown.transform.Rotate(Vector3.right, -maxRotationSpeed * rotationSpeedDownCoeff);
            if (Vector3.Angle(transform.forward, upDown.transform.forward) > maxAngle)
                upDown.transform.localRotation = Quaternion.Euler(-maxAngle, 0, 0);
        }

        left_right_angle = leftRight.transform.rotation.eulerAngles;
        up_down_angle = upDown.transform.rotation.eulerAngles;
    }

    void rotateControlPlaneByMouse()
    {
        float maxAngle = 45;
        float maxRotationSpeed = 3f;

        float rotationSpeedCoeff = 3;
        Vector3 directionCircleDir = directionCircleInWorldWorldPosition - transform.position;

        xAngleBetweenAinAndDirectionCircle = Shared.AngleOffAroundAxis(directionCircleDir, transform.forward, transform.up, true);
        yAngleBetweenAinAndDirectionCircle = Shared.AngleOffAroundAxis(directionCircleDir, transform.forward, transform.right, true);

        //if (yAngleBetweenAinAndDirectionCircle > 0)
        //{
        //    upDown.transform.Rotate(Vector3.right, maxRotationSpeed);
        //    if (Vector3.Angle(transform.forward, upDown.transform.forward) > maxAngle)
        //        upDown.transform.localRotation = Quaternion.Euler(maxAngle, 0, 0);
        //}
        //if (yAngleBetweenAinAndDirectionCircle < 0)
        //{
        //    upDown.transform.Rotate(Vector3.right, -maxRotationSpeed);
        //    if (Vector3.Angle(transform.forward, upDown.transform.forward) > maxAngle)
        //        upDown.transform.localRotation = Quaternion.Euler(-maxAngle, 0, 0);
        //}
        //if (xAngleBetweenAinAndDirectionCircle > 0)
        //{
        //    leftRight.transform.Rotate(Vector3.up, maxRotationSpeed);
        //    if (Vector3.Angle(transform.forward, leftRight.transform.forward) > maxAngle)
        //        leftRight.transform.localRotation = Quaternion.Euler(0, maxAngle, 0);
        //}
        //if (xAngleBetweenAinAndDirectionCircle < 0)
        //{
        //    leftRight.transform.Rotate(Vector3.up, -maxRotationSpeed);
        //    if (Vector3.Angle(transform.forward, leftRight.transform.forward) > maxAngle)
        //        leftRight.transform.localRotation = Quaternion.Euler(0, -maxAngle, 0);
        //}


        // not correct rotation speed at nott big angles
        float upDownTargetAngle = Mathf.Clamp(upDownAngleCoeff * Mathf.Abs(yAngleBetweenAinAndDirectionCircle), 0, maxAngle);

        float additionalFactorForLeftRight = 5 / Mathf.Pow(Mathf.Abs(xAngleBetweenAinAndDirectionCircle), 0.05f * Mathf.Abs(xAngleBetweenAinAndDirectionCircle)) + 1;
        float leftRightTargetAngle = Mathf.Clamp(leftRightAngleCoeff * Mathf.Abs(xAngleBetweenAinAndDirectionCircle) * additionalFactorForLeftRight, 0, maxAngle);

        if (yAngleBetweenAinAndDirectionCircle > 0)
        {
            upDown.transform.Rotate(Vector3.right, maxRotationSpeed);
            if (Vector3.Angle(transform.forward, upDown.transform.forward) > upDownTargetAngle)
                upDown.transform.localRotation = Quaternion.Euler(upDownTargetAngle, 0, 0);
        }
        if (yAngleBetweenAinAndDirectionCircle < 0)
        {
            upDown.transform.Rotate(Vector3.right, -maxRotationSpeed);
            if (Vector3.Angle(transform.forward, upDown.transform.forward) > upDownTargetAngle)
                upDown.transform.localRotation = Quaternion.Euler(-upDownTargetAngle, 0, 0);
        }
        if (xAngleBetweenAinAndDirectionCircle > 0)
        {
            leftRight.transform.Rotate(Vector3.up, maxRotationSpeed);
            if (Vector3.Angle(transform.forward, leftRight.transform.forward) > leftRightTargetAngle)
                leftRight.transform.localRotation = Quaternion.Euler(0, leftRightTargetAngle, 0);
        }
        if (xAngleBetweenAinAndDirectionCircle < 0)
        {
            leftRight.transform.Rotate(Vector3.up, -maxRotationSpeed);
            if (Vector3.Angle(transform.forward, leftRight.transform.forward) > leftRightTargetAngle)
                leftRight.transform.localRotation = Quaternion.Euler(0, -leftRightTargetAngle, 0);
        }
    }

    void addEngineForce()
    {
        engineThrust = engineThrustCoeff * power;
        engineThrustKG = engineThrust / 10;
        rb.AddForce(transform.forward * engineThrust);
    }

    void addControlPlaneForces()
    {
        //controlPlaneDrug = 10;
        float controlPlaneDrag = velocity.magnitude * controlPlaneDragCoeff;

        Vector3 positionForAddForce = transform.position - transform.forward * distForAddControlPlaneForce;

        //if (moveUp)
        //    rb.AddForceAtPosition(-transform.up * controlPlaneDrug, positionForAddForce);
        //if (moveDown)
        //    rb.AddForceAtPosition(transform.up * controlPlaneDrug, positionForAddForce);
        //if (moveLeft)
        //    rb.AddForceAtPosition(transform.right * controlPlaneDrug, positionForAddForce);
        //if (moveRight)
        //    rb.AddForceAtPosition(-transform.right * controlPlaneDrug, positionForAddForce);


        Vector3 forceUpDown = controlPlaneDrag * (Vector3.Reflect(velocity, upDown.transform.up) - velocity);
        rb.AddForceAtPosition(forceUpDown, positionForAddForce);

        Vector3 forceLeftRight = controlPlaneDrag * (Vector3.Reflect(velocity, leftRight.transform.right) - velocity);
        rb.AddForceAtPosition(forceLeftRight, positionForAddForce);
    }

    void addEleuronForces()
    {
        Vector3 positionForAddForceLeftAileron = transform.position - transform.right * distForAddAileronForce;
        Vector3 positionForAddForceRightAileron = transform.position + transform.right * distForAddAileronForce;

        float aileronsDrag = velocity.magnitude * velocity.magnitude * aileronsDragCoeff; // * velocity * angle

        if (leftRoll)
        {
            rb.AddForceAtPosition(-transform.up * aileronsDrag, positionForAddForceLeftAileron);
            rb.AddForceAtPosition(transform.up * aileronsDrag, positionForAddForceRightAileron);
        }
        if (rightRoll)
        {
            rb.AddForceAtPosition(transform.up * aileronsDrag, positionForAddForceLeftAileron);
            rb.AddForceAtPosition(-transform.up * aileronsDrag, positionForAddForceRightAileron);
        }
    }

    void addCorpusForces()
    {
        float upDownDrag = velocity.magnitude * upDownDragCoeff;
        Vector3 forceUpDownPlane = upDownDrag * (Vector3.Reflect(velocity, transform.up) - velocity);
        rb.AddForceAtPosition(forceUpDownPlane, transform.position);

        float leftRightnDrag = velocity.magnitude * leftRightDragCoeff;
        Vector3 forceLeftRightPlane = leftRightnDrag * (Vector3.Reflect(velocity, transform.right) - velocity);
        rb.AddForceAtPosition(forceLeftRightPlane, transform.position);

        float forwardBackDrag = velocity.magnitude * forwardDragCoeff;
        Vector3 forceForwardBackPlane = forwardBackDrag * (Vector3.Reflect(velocity, transform.forward) - velocity);
        rb.AddForceAtPosition(forceForwardBackPlane, transform.position);
    }

    void addWingForces()
    {
        Vector3 liftingForce = velocity.magnitude * velocity.magnitude * transform.up * liftingForceCoeff;
        rb.AddForceAtPosition(liftingForce, transform.position);

        float upDownDrag = velocity.magnitude * upDownWingDragCoeff;
        Vector3 forceUpDownPlane = upDownDrag * (Vector3.Reflect(velocity, transform.up) - velocity);
        rb.AddForceAtPosition(forceUpDownPlane, transform.position);

        float leftRightDrag = velocity.magnitude * leftRightWingDragCoeff;
        Vector3 forceLeftRightPlane = leftRightDrag * (Vector3.Reflect(velocity, transform.right) - velocity);
        rb.AddForceAtPosition(forceLeftRightPlane, transform.position);

        float forwardtDrag = velocity.magnitude * forwardWingDragCoeff;
        Vector3 forceForwardBackPlane = forwardtDrag * (Vector3.Reflect(velocity, transform.forward) - velocity);
        rb.AddForceAtPosition(forceForwardBackPlane, transform.position);
    }

    void addControlPlaneForcesWithEngineWind()
    {
        float controlPlaneDrag = engineThrust * controlPlaneDragEngineWindCoeff;

        Vector3 positionForAddForce = transform.position - transform.forward * distForAddControlPlaneForce;

        Vector3 forceUpDown = controlPlaneDrag * (Vector3.Reflect(transform.forward, upDown.transform.up) - transform.forward);
        rb.AddForceAtPosition(forceUpDown, positionForAddForce);

        Vector3 forceLeftRight = controlPlaneDrag * (Vector3.Reflect(transform.forward, leftRight.transform.right) - transform.forward);
        rb.AddForceAtPosition(forceLeftRight, positionForAddForce);
    }

    void drawControlPlaneForces()
    {
        Vector3 positionDrawForce = transform.position - transform.forward * distForAddControlPlaneForce;
        Vector3 end;

        //if (moveUp)
        //{
        //    end = positionDrawForce + transform.up * 1.5f;
        //    Debug.DrawLine(positionDrawForce, end, new Color(240,94,35));
        //}
        //if (moveDown)
        //{
        //    end = positionDrawForce - transform.up * 1.5f;
        //    Debug.DrawLine(positionDrawForce, end, new Color(240, 94, 35));
        //}
        //if (moveLeft)
        //{
        //    end = positionDrawForce - transform.right * 1.5f;
        //    Debug.DrawLine(positionDrawForce, end, new Color(240, 94, 35));
        //}
        //if (moveRight)
        //{
        //    end = positionDrawForce + transform.right * 1.5f;
        //    Debug.DrawLine(positionDrawForce, end, new Color(240, 94, 35));
        //}

        Vector3 forceUpDown = Vector3.Reflect(velocity, upDown.transform.up) - velocity;
        UnityEngine.Debug.DrawLine(positionDrawForce, positionDrawForce + forceUpDown, Color.magenta);

        Vector3 forceLeftRight = Vector3.Reflect(velocity, leftRight.transform.right) - velocity;
        UnityEngine.Debug.DrawLine(positionDrawForce, positionDrawForce + forceLeftRight, Color.yellow);
    }

    void drawEleuronForces()
    {
        Vector3 positionDrawForce = transform.position - transform.forward * distForAddControlPlaneForce;
        Vector3 end;

        Vector3 positionForDrawForceLeftAileron = transform.position - transform.right * distForAddAileronForce;
        Vector3 positionForDrawForceRightAileron = transform.position + transform.right * distForAddAileronForce;

        if (leftRoll)
        {
            end = positionForDrawForceLeftAileron + transform.up * 1.5f;
            UnityEngine.Debug.DrawLine(positionForDrawForceLeftAileron, end, new Color(240, 94, 35));

            end = positionForDrawForceRightAileron - transform.up * 1.5f;
            UnityEngine.Debug.DrawLine(positionForDrawForceRightAileron, end, new Color(240, 94, 35));
        }
        if (rightRoll)
        {
            end = positionForDrawForceLeftAileron - transform.up * 1.5f;
            UnityEngine.Debug.DrawLine(positionForDrawForceLeftAileron, end, new Color(240, 94, 35));

            end = positionForDrawForceRightAileron + transform.up * 1.5f;
            UnityEngine.Debug.DrawLine(positionForDrawForceRightAileron, end, new Color(240, 94, 35));
        }
    }

    void drawCorpusForces()
    {
        Vector3 forceUpDownPlane = upDownDragCoeff * (Vector3.Reflect(velocity, transform.up) - velocity);
        UnityEngine.Debug.DrawLine(transform.position, transform.position + forceUpDownPlane, Color.magenta);

        Vector3 forceLeftRightPlane = leftRightDragCoeff * (Vector3.Reflect(velocity, transform.right) - velocity);
        UnityEngine.Debug.DrawLine(transform.position, transform.position + forceLeftRightPlane, Color.yellow);

        Vector3 forceForwardBackPlane = forwardDragCoeff * (Vector3.Reflect(velocity, transform.forward) - velocity);
        UnityEngine.Debug.DrawLine(transform.position, transform.position + forceForwardBackPlane, Color.cyan);
    }

    void drawWingForces()
    {
        Vector3 forceUpDownPlane = upDownWingDragCoeff * (Vector3.Reflect(velocity, transform.up) - velocity);
        UnityEngine.Debug.DrawLine(transform.position, transform.position + forceUpDownPlane, Color.magenta);

        Vector3 forceLeftRightPlane = leftRightWingDragCoeff * (Vector3.Reflect(velocity, transform.right) - velocity);
        UnityEngine.Debug.DrawLine(transform.position, transform.position + forceLeftRightPlane, Color.yellow);

        Vector3 forceForwardBackPlane = forwardWingDragCoeff * (Vector3.Reflect(velocity, transform.forward) - velocity);
        UnityEngine.Debug.DrawLine(transform.position, transform.position + forceForwardBackPlane, Color.yellow);
    }

    void calcAcceleration()
    {
        actualAcceleration = (rb.velocity - prevVelocity) / Time.fixedDeltaTime;
        prevVelocity = rb.velocity;
    }
}
