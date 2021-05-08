using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

//http://www.zaretto.com/sites/zaretto.com/files/missile-aerodynamic-data/AIM120C5-Performance-Assessment-rev2.pdf
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

    float maxAcceleration;

    Vector3 velocity;
    float velocityMaggnitude;

    int counterForShoot;
    int counter;

    List<GameObject> infoNearObjList = new List<GameObject>(); // list of cloned prefabs

    GameObject rocketOwner; // use for rocket

    GameObject lastLaunchedRocket;

    bool isPlayer = false;

    GameObject prefabForExplosion;

    Transform trailRenderer;

    bool observeLastLaunchedRocket;
    bool observeLastLaunchedInControlObjRocket;

    // public

    public bool isLaunchedRocket = false; // use for rocket, public for use in rocketLauncher script
    public GameObject target; // public for use in rocketLauncher script

    public Vector3 actualAcceleration;
    public Vector3 prevAcceleration;
    public Vector3 actualJerk;

    public Vector3 localAngularVelocity;
    public float distForAddControlPlaneOrEngineForce;
    public float distForAddAileronForce;
    public Vector3 tensor1;
    public Rigidbody rb;
    public Bullet2 bullet;
    public GameObject rocket;

    public GameObject prefabForShowInfo;

    public LineRenderer lineRenderer;

    public Camera UICam;

    public float initSpeed = 100;

    [Range(0.0f, 100f)] public float power = 0;
    public float engineThrust = 0;

    public Image hpBar;

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

    [Range(0.0f, 100f)] public float hp = 100f;

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
    public AnimationCurve correctPDResultAnimationCurve;
    public AnimationCurve correctPDResultTractionAnimationCurve;

    public float pCoeffLeftRight;
    public float dCoeffLeftRight;

    public float pCoeffUpDown;
    public float dCoeffUpDown;

    void Start()
    {
        prefabForShowInfo = GameObject.Find("for show info near obj");
        UICam = GameObject.Find("UICamera").GetComponent<Camera>();
        hpBar = GameObject.Find("hpBar").GetComponent<Image>();
        rectTransformDirectionCircle = GameObject.Find("direction_circle").GetComponent<RectTransform>();
        rectTransformDirectionCircleArrow = GameObject.Find("direction_arrow").GetComponent<RectTransform>();
        rectTransformEnemyArrow = GameObject.Find("enemy_arrow").GetComponent<RectTransform>();
        rectTransformQuadAroundTarget = GameObject.Find("quad_around_target").GetComponent<RectTransform>();
        speedTextLabel = GameObject.Find("controlObjectSpeed").GetComponent<Text>();
        powerTextLabel = GameObject.Find("controlObjectPower").GetComponent<Text>();
        accelerationTextLabel = GameObject.Find("controlObjectAcceleration").GetComponent<Text>();
        maxAccelerationTextLabel = GameObject.Find("controlObjectMaxAcceleration").GetComponent<Text>();
        attackAngleTextLabel = GameObject.Find("controlObjectAttackAngle").GetComponent<Text>();
        altitudeTextLabel = GameObject.Find("controlObjectAltitude").GetComponent<Text>();
        tractionVectorControlTextLabel = GameObject.Find("controlObjectTractionVectorControl").GetComponent<Text>();

        // find in self object (not in Unity Scene)
        trailRenderer = gameObject.transform.Find("for-trail-renderer");

        //Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;
        if (cam.GetComponent<CameraOperate>().controlObject == gameObject && rocketOwner == null && !Shared.player) { // rocketOwner == null - because bug if press U untill launch rocket
            isPlayer = true;
            Shared.player = gameObject;
        }
        else
            isPlayer = false;

        forwardDirection = GameObject.Find("direction_where_look_control_object").GetComponent<RectTransform>();

        rectTransformDirectionCircle.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);

        rb = GetComponent<Rigidbody>();
        if (!isLaunchedRocket)
            rb.velocity = transform.forward * initSpeed;
        prevVelocity = rb.velocity;
        prevAcceleration = Vector3.zero;
        rb.maxAngularVelocity = 20;
        rb.inertiaTensor = tensor1 * rb.mass;

        directionCircleInWorldWorldPosition = transform.position + transform.forward * aimDistist;

        Cursor.visible = false;

        lineRenderer.positionCount = 2;

        if (isPlayer)
            prepareInfoNearObjArr();
    }

    void FixedUpdate()
    {
        counter++;

        rb.inertiaTensor = tensor1 * rb.mass;

        if (isPlayer)
        {
            shoot();
            showAimPoint();
            calcHP();
        }

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
        calcJerk();
        calcAttackAngle();

        localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity) * Mathf.Rad2Deg; // deg / sec

        rb.angularDrag = 1f * angularDragCoeff * rb.velocity.magnitude + 1f;

        // use for launchedRocket
        destroyRocket();
        offRocketEngine();
    }

    void Update()
    {
        //drawControlPlaneForces();
        //drawEleuronForces();
        //drawCorpusForces();
        //drawWingForces();
        //drawEngineForce();

        if (isLaunchedRocket)
            drawAimPosForRocket();

        if (!isLaunchedRocket)
            handleKeyInput(); // for bot can turn

        if (isPlayer)
        {
            launchRocket();
            //handleKeyInput(); // for bot can not turn

            if (Input.GetKeyDown(KeyCode.U))
                observeLastLaunchedRocket = !observeLastLaunchedRocket;
            if (Input.GetKeyDown(KeyCode.I))
                observeLastLaunchedInControlObjRocket = !observeLastLaunchedInControlObjRocket;

            if (observeLastLaunchedInControlObjRocket && Shared.lastLauncheInControlObjdRocket)
                cam.GetComponent<CameraOperate>().controlObject = Shared.lastLauncheInControlObjdRocket;
            else if (observeLastLaunchedRocket && lastLaunchedRocket)
                cam.GetComponent<CameraOperate>().controlObject = lastLaunchedRocket;
            else
                cam.GetComponent<CameraOperate>().controlObject = gameObject;
        }
    }

    void OnGUI()
    {
    }

    void LateUpdate() // late update - not late fixed update
    {
        if (!isPlayer) return;

        setAimAndDirectionCirclePosition();
        drawArrows();
        drawInfoNearObjects();
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

    void prepareInfoNearObjArr()
    {
        foreach (GameObject obj in Shared.hitWithBulletOrRocketObjects)
        {
            if (obj == gameObject) continue;

            GameObject clone = Instantiate(prefabForShowInfo, Vector3.zero, Quaternion.identity, prefabForShowInfo.transform.parent.transform);
            clone.GetComponent<InfoNearObjClass>().gameObj = obj;
            infoNearObjList.Add(clone);
        }
    }

    public void addObjToInfoNearObjList(GameObject obj) // public for use in rocketLauncher script
    {
        GameObject clone = Instantiate(prefabForShowInfo, Vector3.zero, Quaternion.identity, prefabForShowInfo.transform.parent.transform);
        clone.GetComponent<InfoNearObjClass>().gameObj = obj;
        infoNearObjList.Add(clone);
    }

    void drawInfoNearObjects()
    {
        List<GameObject> objListForRemove = new List<GameObject>();

        foreach (GameObject obj in infoNearObjList)
        {
            try
            {
                Color textColor = Color.blue;
                if (obj.GetComponent<InfoNearObjClass>().gameObj == target)
                    textColor = Color.red;

                Vector3 wrapObjPosition = Camera.main.WorldToScreenPoint(obj.GetComponent<InfoNearObjClass>().gameObj.transform.position);

                GameObject textForDist = obj.transform.GetChild(0).gameObject;
                float dist = Vector3.Distance(cam.transform.position, obj.GetComponent<InfoNearObjClass>().gameObj.transform.position);
                textForDist.GetComponent<Text>().color = textColor;
                textForDist.GetComponent<Text>().text = "dist: " + dist.ToString("0");

                GameObject textForVelocity = obj.transform.GetChild(2).gameObject;
                float velocity = obj.GetComponent<InfoNearObjClass>().gameObj.GetComponent<controlObject>().rb.velocity.magnitude * 3.6f;
                textForVelocity.GetComponent<Text>().color = textColor;
                textForVelocity.GetComponent<Text>().text = "spd: " + velocity.ToString("0");

                GameObject textForAcceleration = obj.transform.GetChild(3).gameObject;
                float acceleration = obj.GetComponent<InfoNearObjClass>().gameObj.GetComponent<controlObject>().actualAcceleration.magnitude / Physics.gravity.magnitude;
                textForAcceleration.GetComponent<Text>().color = textColor;
                textForAcceleration.GetComponent<Text>().text = "acc: " + acceleration.ToString("0");

                if (wrapObjPosition.z > 0)
                {
                    wrapObjPosition.z = 0;
                    obj.GetComponent<RectTransform>().transform.position = wrapObjPosition;
                }
                else
                    obj.GetComponent<RectTransform>().transform.position = new Vector3(3000, 0, 0);

                // hp
                Image hpBar = obj.transform.GetChild(1).gameObject.transform.GetChild(1).gameObject.GetComponent<Image>();
                if (obj.GetComponent<InfoNearObjClass>().gameObj.GetComponent<controlObject>() != null)
                    hpBar.fillAmount = obj.GetComponent<InfoNearObjClass>().gameObj.GetComponent<controlObject>().hp / 100;
            }
            catch
            {
                objListForRemove.Add(obj);
            }
        }

        foreach (GameObject obj in objListForRemove)
            Destroy(obj);

        infoNearObjList.RemoveAll(objListForRemove.Contains);
    }

    void setAimAndDirectionCirclePosition()
    {
        aimInWorldSpacPosition = transform.position + transform.forward * aimDistist;
        Vector3 forwardDirectionPos = cam.WorldToScreenPoint(aimInWorldSpacPosition);
        if (forwardDirectionPos.z < 0)
        {
            forwardDirection.position = new Vector3(3000, 3000, 0);
        }
        else
        {
            forwardDirectionPos.z = 0;
            forwardDirection.position = forwardDirectionPos;
        }

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
        if (target)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(target.transform.position);
            if (screenPos.z < 0) return;
            screenPos.z = 0;
            rectTransformQuadAroundTarget.position = screenPos;
        }
        else
        {
            rectTransformQuadAroundTarget.position = new Vector3(3000, 3000, 0);
        }
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
        counterForShoot++;
        if (Input.GetMouseButton(0) && counterForShoot > 0) // GetMouseButton GetMouseButtonDown
        {
            counterForShoot = 0;
            Bullet2 bulletClone = Instantiate(bullet, new Vector3(transform.position.x, transform.position.y, transform.position.z) + transform.forward * 1, transform.rotation);
            bulletClone.speed = bulletClone.initBulletSpeed * new Vector3(transform.forward.x, transform.forward.y, transform.forward.z) + rb.velocity;
            bulletClone.owner = gameObject;
        }
    }

    void launchRocket()
    {
        if (Input.GetKeyDown(KeyCode.Z) && target)
        {
            GameObject rocketClone = Instantiate(rocket, new Vector3(transform.position.x, transform.position.y, transform.position.z) - transform.up * 1.0f, transform.rotation);
            lastLaunchedRocket = rocketClone;
            rocketClone.GetComponent<controlObject>().isLaunchedRocket = true;
            rocketClone.GetComponent<controlObject>().rb.velocity = rb.velocity;
            rocketClone.GetComponent<controlObject>().target = target;
            rocketClone.GetComponent<controlObject>().rocketOwner = gameObject;
            addObjToInfoNearObjList(rocketClone);
            Shared.hitWithBulletOrRocketObjects.Add(rocketClone);
        }
    }

    void offRocketEngine()
    {
        if (isLaunchedRocket && trailRenderer && counter > 30000)
        {
            power = 0;
            trailRenderer.gameObject.SetActive(false);
        }
    }

    void destroyRocket()
    {
        if (isLaunchedRocket && counter > 250000)
            destroyLaunchedRocket();

        if (isLaunchedRocket && !target)
            destroyLaunchedRocket();

        // destroy If Target Near
        if (isLaunchedRocket && target)
        {
            Vector3 forward = transform.TransformDirection(rb.velocity);
            Vector3 toOther = target.transform.position - transform.position;

            float distWhenRocketHitTarget = 20;

            float angle = Vector3.Angle(target.transform.position - transform.position, rb.velocity);

            if (/*Vector3.Dot(forward, toOther) < 0*/ angle > 190 && Vector3.Distance(transform.position, target.transform.position) < 100 ||
                Vector3.Distance(transform.position, target.transform.position) < distWhenRocketHitTarget) 
            {
                int iterationCt = 4;
                float minDistance = 1000;

                for (var j = 0; j < iterationCt; j++)
                {
                    Vector3 rocketPosition = transform.position + (rb.velocity.magnitude / iterationCt) * rb.velocity.normalized * j * Time.fixedDeltaTime;
                    controlObject targetObj = target.GetComponent<controlObject>();
                    Vector3 targetPosition = target.transform.position + (targetObj.rb.velocity.magnitude / iterationCt) * targetObj.rb.velocity.normalized * j * Time.fixedDeltaTime;
                    float distance = Vector3.Distance(rocketPosition, targetPosition);
                    if (distance < minDistance)
                        minDistance = distance;
                }
                //minDistance = Vector3.Distance(transform.position, target.transform.position);

                float damage;
                if (minDistance > distWhenRocketHitTarget)
                    damage = 0;
                else
                    damage = (20 - minDistance) * 5;

                target.GetComponent<controlObject>().hp -= damage;
                destroyLaunchedRocket();
            }
        }
    }

    void destroyLaunchedRocket()
    {
        //Debug.Break(); // pauses at the end of frame, not the point in code you put it
        prefabForExplosion = GameObject.Find("Explosion");
        GameObject cloneExplosion = Instantiate(prefabForExplosion, gameObject.transform.position, Quaternion.identity);
        Destroy(gameObject);
        Destroy(cloneExplosion, 5);
    }

    // disable another controlObjects
    void showAimPoint()
    {
        if (target)
        {
            Vector3 targetSpeed;

            if (target.GetComponent<Rigidbody>().velocity != null)
                targetSpeed = target.GetComponent<Rigidbody>().velocity;
            else if (target.GetComponent<target>().calculatedSpeed != null)
                targetSpeed = target.GetComponent<target>().calculatedSpeed;
            else
                targetSpeed = Vector3.zero;

            Vector3 aimPosition = Shared.CalculateAim(target.transform.position, targetSpeed, transform.position, bullet.initBulletSpeed, rb.velocity, Vector3.zero);
            lineRenderer.SetPosition(0, target.transform.position);
            lineRenderer.SetPosition(1, aimPosition);
        }
        else
        {
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
        }
    }

    void findNearestToScreenCenterObj()
    {
        GameObject closest = null;
        float dot = -2;

        foreach (GameObject obj in Shared.hitWithBulletOrRocketObjects)
        {
            if (obj == gameObject || !obj) continue;

            Vector3 localPoint = Camera.main.transform.InverseTransformPoint(obj.transform.position).normalized;
            Vector3 forward = Vector3.forward;
            float test = Vector3.Dot(localPoint, forward);
            if (test > dot)
            {
                dot = test;
                closest = obj;
            }
        }


        target = closest;
    }

    void calcAttackAngle()
    {
        attackAngle = Vector3.Angle(transform.forward, rb.velocity);
    }

    void renderTransformGizmos()
    {
        float gizmosSize = 5;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * gizmosSize * 1);

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

        if (Input.GetKeyDown(KeyCode.C) && isPlayer)
            findNearestToScreenCenterObj();

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

    void rotateControlPlaneByMouse() // use for rocket aiming too (not only rotate by mouse)
    {
        float yAngleBetweenForwardAndDirectionCircle;
        float xAngleBetweenForwardAndDirectionCircle;

        Vector3 aimPosition = Vector3.zero;

        if (isLaunchedRocket && target)
        {
            Vector3 targetAcceleration = Vector3.zero;
            try
            {
                targetAcceleration = target.GetComponent<controlObject>().actualAcceleration;
            }
            catch { }
            aimPosition = Shared.CalculateAim(target.transform.position, target.GetComponent<controlObject>().rb.velocity, transform.position, rb.velocity.magnitude, Vector3.zero, targetAcceleration);
            Vector3 direction = aimPosition - transform.position;

            //yAngleBetweenForwardAndDirectionCircle = Shared.AngleOffAroundAxis(direction, transform.forward, transform.up, true);
            //xAngleBetweenForwardAndDirectionCircle = Shared.AngleOffAroundAxis(direction, transform.forward, transform.right, true);

            yAngleBetweenForwardAndDirectionCircle = Shared.AngleOffAroundAxis(direction, rb.velocity, transform.up, true);
            xAngleBetweenForwardAndDirectionCircle = Shared.AngleOffAroundAxis(direction, rb.velocity, transform.right, true);
        }
        else
        {
            Vector3 directionCircleDir = directionCircleInWorldWorldPosition - transform.position;
            yAngleBetweenForwardAndDirectionCircle = Shared.AngleOffAroundAxis(directionCircleDir, transform.forward, transform.up, true);
            xAngleBetweenForwardAndDirectionCircle = Shared.AngleOffAroundAxis(directionCircleDir, transform.forward, transform.right, true);
        }

        float correctResultUpDown =    correctPDResultAnimationCurve.Evaluate(Mathf.Abs(xAngleBetweenForwardAndDirectionCircle));
        float correctResultLeftRight = correctPDResultAnimationCurve.Evaluate(Mathf.Abs(yAngleBetweenForwardAndDirectionCircle));
        if (tractionVectorControlEnabled)
        {
            float value = correctPDResultTractionAnimationCurve.Evaluate(rb.velocity.magnitude);
            correctResultUpDown *= value;
            correctResultLeftRight *= value;
        }

        float PDUpDownResult =    Shared.PDController(xAngleBetweenForwardAndDirectionCircle, localAngularVelocity.x, pCoeffUpDown    * correctResultUpDown,    dCoeffUpDown);
        float PDLeftRightResult = Shared.PDController(yAngleBetweenForwardAndDirectionCircle, localAngularVelocity.y, pCoeffLeftRight * correctResultLeftRight, dCoeffLeftRight);

        // so that at high speeds it does not shake
        PDUpDownResult    *= 1 - (rb.velocity.magnitude / 2000);
        PDLeftRightResult *= 1 - (rb.velocity.magnitude / 2000);

        // so that the rocket does not spin in place
        if (isLaunchedRocket && target)
        {
            PDUpDownResult    *= 1 - ((Mathf.Abs(attackAngle) * 1.7f + Mathf.Abs(localAngularVelocity.x) * 1.2f) / 350);
            PDLeftRightResult *= 1 - ((Mathf.Abs(attackAngle) * 1.7f + Mathf.Abs(localAngularVelocity.y) * 1.2f) / 350);

            PDUpDownResult    *= 0.0000001f * Mathf.Pow(rb.velocity.magnitude, 2.7f) + 0.1f;
            PDLeftRightResult *= 0.0000001f * Mathf.Pow(rb.velocity.magnitude, 2.7f) + 0.1f;

            if (Vector3.Distance(transform.position, aimPosition) > 4000)
            {
                PDUpDownResult *= 0.2f;
                PDLeftRightResult *= 0.2f;
            }
            else
            {
                PDUpDownResult *= 1 - ((Vector3.Distance(transform.position, aimPosition)) / 5000);
                PDLeftRightResult *= 1 - ((Vector3.Distance(transform.position, aimPosition)) / 5000);
            }
        }       

        float upDownTargetAngle = Mathf.Clamp(PDUpDownResult, -maxControlPlaneAngle, maxControlPlaneAngle);
        float leftRightTargetAngle = Mathf.Clamp(PDLeftRightResult, -maxControlPlaneAngle, maxControlPlaneAngle);

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

        //if (!isPlayer && !isLaunchedRocket) // bots rotate left
        //{
        //    leftRight.transform.localRotation = Quaternion.Euler(0, 25, 0);
        //}
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

    void drawAimPosForRocket()
    {
        if (target)
        {
            Vector3 aimPosition = Shared.CalculateAim(target.transform.position, target.GetComponent<controlObject>().rb.velocity, transform.position, rb.velocity.magnitude, Vector3.zero, Vector3.zero);
            UnityEngine.Debug.DrawLine(transform.position, aimPosition, Color.yellow);
        }
    }


    void calcHP()
    {
        hpBar.fillAmount = hp / 100;
    }

    void calcAcceleration()
    {
        actualAcceleration = (rb.velocity - prevVelocity) / Time.fixedDeltaTime;
        prevVelocity = rb.velocity;

        if (actualAcceleration.magnitude / Physics.gravity.magnitude > maxAcceleration)
            maxAcceleration = actualAcceleration.magnitude / Physics.gravity.magnitude;
    }

    void calcJerk()
    {
        actualJerk = (actualAcceleration - prevAcceleration) / Time.fixedDeltaTime;
        prevAcceleration = actualAcceleration;
    }
}
