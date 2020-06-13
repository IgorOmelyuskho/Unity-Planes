using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class controlObject : MonoBehaviour
{
    Camera cam;
    float mouseSensitivity = 23;
    RectTransform forwardDirection;
    Vector3 prevVelocity;
    float attackAngle;

    float aimDistist = 100000;
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

    GameObject target;

    Vector3 actualAcceleration;
    float maxAcceleration;

    Vector3 velocity;
    float velocityMaggnitude;


    // public
    public float distForAddControlPlaneOrEngineForce;
    public float distForAddAileronForce;
    public Vector3 tensor1;
    public Rigidbody rb;
    public Bullet2 bullet;
    public Vector3 localAngularVelocity;

    public LineRenderer lineRenderer;

    public Camera UICam;

    public float initSpeed = 100;

    [Range(0.0f, 100f)] public float power = 0;
    public float engineThrust = 0;

    public RectTransform rectTransformDirectionCircle;
    public RectTransform rectTransformDirectionCircleArrow;
    public RectTransform rectTransformEnemyArrow;
    public RectTransform rectTransformQuadAroundTarget;

    public Text speedTextLabel;
    public Text powerTextLabel;
    public Text accelerationTextLabel;
    public Text maxAccelerationTextLabel;
    public Text attackAngleTextLabel;
    public Text altitudeTextLabel;
    public Text tractionVectorControlTextLabel;

    public GameObject leftRight;
    public GameObject upDown;
    public GameObject wing;
    public GameObject leftAileron;
    public GameObject rightAileron;

    public bool isPropPlane;
    public bool tractionVectorControlEnabled;

    // если = 0 - тоже самое что вектор тяги неуправляемый, если = 1 - поворачивается также как и рули направления/высоты, можно задать больше 1
    public float tractionVectorControlCoeff = 0.3f;

    public float maxControlPlaneAngle = 45;
    public float maxControlPlaneRotationSpeed = 5f;

    public float engineThrustCoeff;
    public float engineThrustKG;
    public float controlPlaneDragCoeff;
    public float upDownCorpusDragCoeff;
    public float leftRightCorpusDragCoeff;
    public float forwardDragCoeff;
    public float aileronsDragCoeff;
    public float liftingForceCoeff;
    public float upDownWingDragCoeff;
    public float leftRightWingDragCoeff;
    public float forwardWingDragCoeff;
    public float angularDragCoeff;
    public float controlPlaneDragEngineWindCoeff;

    public float pCoeffLeftRight;
    public float dCoeffLeftRight;

    public float pCoeffUpDown;
    public float dCoeffUpDown;

    void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;

        forwardDirection = GameObject.Find("direction_where_look_control_object").GetComponent<RectTransform>();

        rectTransformDirectionCircle.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);

        rb = GetComponent<Rigidbody>();
        rb.velocity = transform.forward * initSpeed;
        prevVelocity = rb.velocity;
        rb.maxAngularVelocity = 20;
        rb.inertiaTensor = tensor1 * rb.mass;

        directionCircleInWorldWorldPosition = transform.position + transform.forward * aimDistist;

        Cursor.visible = false;

        lineRenderer.positionCount = 2;

        //mesh = GetComponent<MeshFilter>().mesh;
    }

    void FixedUpdate()
    {
        rb.inertiaTensor = tensor1 * rb.mass;

        shoot();
        showAimPoint();

        velocity = rb.velocity;
        velocityMaggnitude = rb.velocity.magnitude;



        if (moveLeft || moveRight || moveDown || moveUp)
            rotateControlPlaneByKey();
        else
            rotateControlPlaneByMouse();

        addEngineForce();
        addCorpusForces();
        addEleuronForces();
        addWingForces();
        addControlPlaneForces();
        if (isPropPlane)
            addControlPlaneForcesWithEngineWind();

        calcAcceleration();
        calcAttackAngle();

        localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity) * Mathf.Rad2Deg; // deg / sec

        rb.angularDrag = angularDragCoeff * rb.velocity.magnitude + 1f;
    }

    void Update()
    {
        //drawControlPlaneForces();
        //drawEleuronForces();
        //drawCorpusForces();
        //drawWingForces();
        //drawEngineForce();
        handleKeyInput();
    }

    void OnGUI()
    {
    }

    void LateUpdate() // late update - not late fixed update
    {
        setAimAndDirectionCirclePosition();
        drawArrows();
        drawQuadAroundTarget();

        if (speedTextLabel)
            speedTextLabel.text = "velocity: " + (velocity.magnitude * 3.6).ToString("0.0") + " km/h";

        if (powerTextLabel)
            powerTextLabel.text = "power: " + power.ToString("0");

        if (accelerationTextLabel)
            accelerationTextLabel.text = "acceleration (G): " + (actualAcceleration.magnitude / Physics.gravity.magnitude).ToString("0.0");

        if (maxAccelerationTextLabel)
            maxAccelerationTextLabel.text = "max acceleration (G): " + maxAcceleration.ToString("0.0");

        if (attackAngleTextLabel)
            attackAngleTextLabel.text = "attack angle: " + attackAngle.ToString("0.0");

        if (altitudeTextLabel)
            altitudeTextLabel.text = "altitude: " + transform.position.y.ToString("0");

        if (tractionVectorControlTextLabel)
            tractionVectorControlTextLabel.text = "traction vector control: " + (tractionVectorControlEnabled ? "on" : "off");
    }

    void OnDrawGizmos() /*OnDrawGizmosSelected  DrawLine*/
    {
        renderTransformGizmos();
        renderVelocityGizmos();

        //Gizmos.DrawRay(transform.position - transform.forward * distForAddControlPlaneForce, transform.up);

        //Bounds bounds = mesh.bounds;
    }

    void setAimAndDirectionCirclePosition()
    {
        aimInWorldSpacPosition = transform.position + transform.forward * aimDistist;
        Vector3 forwardDirectionPos = cam.WorldToScreenPoint(aimInWorldSpacPosition);
        forwardDirectionPos.z = 0;
        forwardDirection.position = forwardDirectionPos;

        if (!Input.GetKey(KeyCode.Space))
        {
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X") * mouseSensitivity, Input.GetAxis("Mouse Y") * mouseSensitivity);
            if (mouseDelta.x != 0 || mouseDelta.y != 0)
            {
                directionCircleInWorldWorldPosition = cam.ScreenToWorldPoint(new Vector3(rectTransformDirectionCircle.position.x + mouseDelta.x, rectTransformDirectionCircle.position.y + mouseDelta.y, aimDistist));
            }
            Vector3 directionCirclePosition = cam.WorldToScreenPoint(directionCircleInWorldWorldPosition);
            directionCirclePosition.z = 0;
            rectTransformDirectionCircle.position = directionCirclePosition;
        }
        else
        {
            directionCircleInWorldWorldPosition = aimInWorldSpacPosition;
            rectTransformDirectionCircle.position = forwardDirectionPos;
        }
    }

    void drawArrows()
    {
        drawArrowIfObjectOutsideScreens(directionCircleInWorldWorldPosition, rectTransformDirectionCircleArrow);

        if (target) 
            drawArrowIfObjectOutsideScreens(target.transform.position, rectTransformEnemyArrow);
    }

    void drawQuadAroundTarget()
    {
        Vector3 screenPos = cam.WorldToScreenPoint(target.transform.position);
        if (screenPos.z < 0) return; 
        screenPos.z = 0;
        rectTransformQuadAroundTarget.position = screenPos;
    }

    void drawArrowIfObjectOutsideScreens(Vector3 targetPosition, RectTransform pointerRectTransform)
    {
        float borderSize = 5f;
        Vector3 tragetPositionScreenPoint = cam.WorldToScreenPoint(targetPosition);
        bool isOffsetScreen = tragetPositionScreenPoint.x <= borderSize || tragetPositionScreenPoint.x >= Screen.width - borderSize || tragetPositionScreenPoint.y <= borderSize || tragetPositionScreenPoint.y >= Screen.height - borderSize;

        if (isOffsetScreen /*|| tragetPositionScreenPoint.z < 0*/)
        {
            Vector3 cappedTargetScreenPosition = tragetPositionScreenPoint;
            if (cappedTargetScreenPosition.x <= borderSize) cappedTargetScreenPosition.x = borderSize;
            if (cappedTargetScreenPosition.x >= Screen.width - borderSize) cappedTargetScreenPosition.x = Screen.width - borderSize;
            if (cappedTargetScreenPosition.y <= borderSize) cappedTargetScreenPosition.y = borderSize;
            if (cappedTargetScreenPosition.y >= Screen.height - borderSize) cappedTargetScreenPosition.y = Screen.height - borderSize;

            Vector3 pointerWorldPosition = UICam.ScreenToWorldPoint(cappedTargetScreenPosition);
            pointerRectTransform.position = pointerWorldPosition;
            pointerRectTransform.localPosition = new Vector3(pointerRectTransform.localPosition.x, pointerRectTransform.localPosition.y, 0f);

            Vector2 arrowPosInScreen = pointerRectTransform.localPosition;
            float angle = Vector2.SignedAngle(Vector2.right, arrowPosInScreen);
            //if (tragetPositionScreenPoint.z < 0)
            //    angle = angle + 180;
            pointerRectTransform.localEulerAngles = new Vector3(0, 0, angle);
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
            Bullet2 bulletClone = Instantiate(bullet, new Vector3(transform.position.x, transform.position.y, transform.position.z) + transform.forward * 1, transform.rotation);
            bulletClone.speed = bulletClone.initBulletSpeed * new Vector3(transform.forward.x, transform.forward.y, transform.forward.z) + rb.velocity;
            bulletClone.owner = gameObject;
        }
    }

    void showAimPoint()
    {
        //foreach (GameObject hitWithBulletObject in Shared.hitWithBulletObjects)
        //{
        //    if (gameObject != hitWithBulletObject)
        //    {
        //        target = hitWithBulletObject;
        //        Vector3 aimPosition = Shared.CalculateAim(target.transform.position, target.GetComponent<Rigidbody>().velocity, transform.position, bullet.initBulletSpeed, rb.velocity);
        //        lineRenderer.SetPosition(0, target.transform.position);
        //        lineRenderer.SetPosition(1, aimPosition);
        //        return;
        //    }
        //}

        target = GameObject.FindGameObjectWithTag("TargetTag");
        Vector3 aimPosition = Shared.CalculateAim(target.transform.position, target.GetComponent<target>().calculatedSpeed, transform.position, bullet.initBulletSpeed, rb.velocity);
        lineRenderer.SetPosition(0, target.transform.position);
        lineRenderer.SetPosition(1, aimPosition);
    }

    void calcAttackAngle()
    {
        attackAngle = Vector3.Angle(transform.forward, rb.velocity);
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

        if (Input.GetKeyDown(KeyCode.V))
            tractionVectorControlEnabled = !tractionVectorControlEnabled;

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
        float rotationSpeedCoeff = 3;

        float rotationSpeedRightCoeff = Mathf.Clamp01(rotationSpeedCoeff * timePressRight);
        float rotationSpeedLeftCoeff = Mathf.Clamp01(rotationSpeedCoeff * timePressLeft);
        float rotationSpeedUpCoeff = Mathf.Clamp01(rotationSpeedCoeff * timePressUp);
        float rotationSpeedDownCoeff = Mathf.Clamp01(rotationSpeedCoeff * timePressDown);

        if (moveRight)
        {
            leftRight.transform.Rotate(Vector3.up, -maxControlPlaneRotationSpeed * rotationSpeedRightCoeff);
            if (Vector3.Angle(transform.forward, leftRight.transform.forward) > maxControlPlaneAngle)
                leftRight.transform.localRotation = Quaternion.Euler(0, -maxControlPlaneAngle, 0);
        }
        if (moveLeft)
        {
            leftRight.transform.Rotate(Vector3.up, maxControlPlaneRotationSpeed * rotationSpeedLeftCoeff);
            if (Vector3.Angle(transform.forward, leftRight.transform.forward) > maxControlPlaneAngle)
                leftRight.transform.localRotation = Quaternion.Euler(0, maxControlPlaneAngle, 0);
        }
        if (moveUp)
        {
            upDown.transform.Rotate(Vector3.right, maxControlPlaneRotationSpeed * rotationSpeedUpCoeff);
            if (Vector3.Angle(transform.forward, upDown.transform.forward) > maxControlPlaneAngle)
                upDown.transform.localRotation = Quaternion.Euler(maxControlPlaneAngle, 0, 0);
        }
        if (moveDown)
        {
            upDown.transform.Rotate(Vector3.right, -maxControlPlaneRotationSpeed * rotationSpeedDownCoeff);
            if (Vector3.Angle(transform.forward, upDown.transform.forward) > maxControlPlaneAngle)
                upDown.transform.localRotation = Quaternion.Euler(-maxControlPlaneAngle, 0, 0);
        }
    }

    void rotateControlPlaneByMouse()
    {
        Vector3 directionCircleDir = directionCircleInWorldWorldPosition - transform.position;

        float yAngleBetweenForwardAndDirectionCircle = Shared.AngleOffAroundAxis(directionCircleDir, transform.forward, transform.up, true);
        float xAngleBetweenForwardAndDirectionCircle = Shared.AngleOffAroundAxis(directionCircleDir, transform.forward, transform.right, true);

        float PDUpDownResult = Shared.PDController(xAngleBetweenForwardAndDirectionCircle, localAngularVelocity.x, pCoeffUpDown, dCoeffUpDown);
        float PDLrftRightResult = Shared.PDController(yAngleBetweenForwardAndDirectionCircle, localAngularVelocity.y, pCoeffLeftRight, dCoeffLeftRight);

        PDUpDownResult *= 1 - (rb.velocity.magnitude / 2000);
        PDLrftRightResult *= 1 - (rb.velocity.magnitude / 2000);

        float upDownTargetAngle = Mathf.Clamp(PDUpDownResult, -maxControlPlaneAngle, maxControlPlaneAngle);
        float leftRightTargetAngle = Mathf.Clamp(PDLrftRightResult, -maxControlPlaneAngle, maxControlPlaneAngle);

        float upDownControlPlaneSignedAngle = Shared.GetSignedAngle(transform.rotation, upDown.transform.rotation, transform.right);
        float leftRightControlPlaneSignedAngle = Shared.GetSignedAngle(transform.rotation, leftRight.transform.rotation, transform.up);

        float upDownDeltaAngle = upDownTargetAngle - upDownControlPlaneSignedAngle;
        float leftRightDeltaAngle = leftRightTargetAngle - leftRightControlPlaneSignedAngle;

        if (upDownDeltaAngle > 0)
        {
            if (Mathf.Abs(upDownDeltaAngle) > maxControlPlaneRotationSpeed)
                upDown.transform.Rotate(Vector3.right, maxControlPlaneRotationSpeed);
            else
                upDown.transform.localRotation = Quaternion.Euler(upDownTargetAngle, 0, 0);
        }
        if (upDownDeltaAngle < 0)
        {
            if (Mathf.Abs(upDownDeltaAngle) > maxControlPlaneRotationSpeed)
                upDown.transform.Rotate(Vector3.right, -maxControlPlaneRotationSpeed);
            else
                upDown.transform.localRotation = Quaternion.Euler(upDownTargetAngle, 0, 0);
        }
        if (leftRightDeltaAngle > 0)
        {
            if (Mathf.Abs(leftRightDeltaAngle) > maxControlPlaneRotationSpeed)
                leftRight.transform.Rotate(Vector3.up, maxControlPlaneRotationSpeed);
            else
                leftRight.transform.localRotation = Quaternion.Euler(0, leftRightTargetAngle, 0);
        }
        if (leftRightDeltaAngle < 0)
        {
            if (Mathf.Abs(leftRightDeltaAngle) > maxControlPlaneRotationSpeed)
                leftRight.transform.Rotate(Vector3.up, -maxControlPlaneRotationSpeed);
            else
                leftRight.transform.localRotation = Quaternion.Euler(0, leftRightTargetAngle, 0);
        }
    }

    void addEngineForce()
    {
        Vector3 force;

        Vector3 positionForAddForce = transform.position - transform.forward * distForAddControlPlaneOrEngineForce;

        engineThrust = engineThrustCoeff * power;
        engineThrustKG = engineThrust / 10;

        if (tractionVectorControlEnabled)
            force = engineThrust * Vector3.SlerpUnclamped( transform.forward, Vector3.Slerp(upDown.transform.forward, leftRight.transform.forward, 0.5f), tractionVectorControlCoeff);
        else
            force = engineThrust * transform.forward;

        rb.AddForceAtPosition(force, positionForAddForce);
    }

    void addControlPlaneForces()
    {
        //float controlPlaneDrag = 1000;
        float controlPlaneDrag = velocity.magnitude * controlPlaneDragCoeff;

        Vector3 positionForAddForce = transform.position - transform.forward * distForAddControlPlaneOrEngineForce;

        //if (moveUp)
        //    rb.AddForceAtPosition(-transform.up * controlPlaneDrag, positionForAddForce);
        //if (moveDown)
        //    rb.AddForceAtPosition(transform.up * controlPlaneDrag, positionForAddForce);
        //if (moveLeft)
        //    rb.AddForceAtPosition(transform.right * controlPlaneDrag, positionForAddForce);
        //if (moveRight)
        //    rb.AddForceAtPosition(-transform.right * controlPlaneDrag, positionForAddForce);


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
        float upDownDrag = velocity.magnitude * upDownCorpusDragCoeff;
        Vector3 forceUpDownPlane = upDownDrag * (Vector3.Reflect(velocity, transform.up) - velocity);
        rb.AddForceAtPosition(forceUpDownPlane, transform.position);

        float leftRightnDrag = velocity.magnitude * upDownCorpusDragCoeff;
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

        Vector3 positionForAddForce = transform.position - transform.forward * distForAddControlPlaneOrEngineForce;

        Vector3 forceUpDown = controlPlaneDrag * (Vector3.Reflect(transform.forward, upDown.transform.up) - transform.forward);
        rb.AddForceAtPosition(forceUpDown, positionForAddForce);

        Vector3 forceLeftRight = controlPlaneDrag * (Vector3.Reflect(transform.forward, leftRight.transform.right) - transform.forward);
        rb.AddForceAtPosition(forceLeftRight, positionForAddForce);
    }




    void drawControlPlaneForces()
    {
        Vector3 positionDrawForce = transform.position - transform.forward * distForAddControlPlaneOrEngineForce;
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
        Vector3 positionDrawForce = transform.position - transform.forward * distForAddControlPlaneOrEngineForce;
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
        Vector3 forceUpDownPlane = upDownCorpusDragCoeff * (Vector3.Reflect(velocity, transform.up) - velocity);
        UnityEngine.Debug.DrawLine(transform.position, transform.position + forceUpDownPlane, Color.magenta);

        Vector3 forceLeftRightPlane = leftRightCorpusDragCoeff * (Vector3.Reflect(velocity, transform.right) - velocity);
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

    void drawEngineForce()
    {
        Vector3 positionForAddForce = transform.position - transform.forward * distForAddControlPlaneOrEngineForce;
        Vector3 force;

        if (tractionVectorControlEnabled)
            force = 5 * Vector3.SlerpUnclamped( transform.forward, Vector3.Slerp(upDown.transform.forward, leftRight.transform.forward, 0.5f), tractionVectorControlCoeff);
        else
            force = 5 * transform.forward;

        UnityEngine.Debug.DrawLine(positionForAddForce, positionForAddForce + force, Color.magenta);
    }




    void calcAcceleration()
    {
        actualAcceleration = (rb.velocity - prevVelocity) / Time.fixedDeltaTime;
        prevVelocity = rb.velocity;

        if (actualAcceleration.magnitude / Physics.gravity.magnitude > maxAcceleration)
            maxAcceleration = actualAcceleration.magnitude / Physics.gravity.magnitude;
    }
}
