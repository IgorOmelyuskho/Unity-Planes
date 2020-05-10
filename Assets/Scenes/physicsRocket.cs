using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class physicsRocket : MonoBehaviour
{
    public Rigidbody rb;

    Vector3 prevVelocity;
    public Vector3 velocity;
    public float velocityMaggnitude;

    public Vector3 angularVelocity;

    public Vector3 actualAcceleration;

    public float attackAngle;

    public float distForAddForce = 0.5f;
    public Vector3 tensor1 = new Vector3(1f, 1f, 0.2f);

    [Range(0.0f, 100f)] public float power = 0;
    public float engineThrust = 0;

    public Text speedTextLabel;
    public Text powerTextLabel;
    public Text accelerationTextLabel;
    public Text attackAngleTextLabel;

    public bool drawVelocityGizmos;

    bool moveLeft;
    bool moveRight;
    bool moveUp;
    bool moveDown;
    bool leftRoll;
    bool rightRoll;

    public Vector3 left_right_angle;
    public Vector3 left_right_attack_angle;
    public float left_right_attack_angle_magnitude;
    public Vector3 up_down_angle;
    public Vector3 up_down_attack_angle;
    public float up_down_attack_angle_magnitude;

    public GameObject leftRight;
    public GameObject upDown;
    public GameObject wing;
    public GameObject leftEleron;
    public GameObject rightEleron;

    float controlPlaneDrag;
    float eleronsDrag;

    public float engineThrustCoeff;
    public float engineThrustKG;
    public float controlPlaneDragCoeff;
    public float upDownDragCoeff;
    public float leftRightDragCoeff;
    public float forwardDragCoeff;
    public float eleronsDragCoeff;
    public float liftingForceCoeff;
    public float upDownWingDragCoeff;
    public float leftRightWingDragCoeff;
    public float forwardWingDragCoeff;
    public float angularDragCoeff;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.velocity = new Vector3(0, 0, 55);
        rb.maxAngularVelocity = 20;
        rb.inertiaTensor = tensor1 * rb.mass;
        //rb.inertiaTensorRotation = Quaternion.identity;

        //rb.AddTorque(transform.up * 100);
        //rb.AddRelativeTorque(transform.up * 100);
    }


    void FixedUpdate()
    {
        rb.inertiaTensor = tensor1 * rb.mass;
        handleKeyInput();

        velocity = rb.velocity;
        velocityMaggnitude = rb.velocity.magnitude;

        rotateControlPlane();

        addEngineForce();
        addCorpusForces();
        addControlPlaneForces();
        addEleuronForces();
        addWingForces();

        calcAcceleration();
        calcAttackAngles();

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
        if (speedTextLabel)
            speedTextLabel.text = "velocity: " + velocity.magnitude.ToString("0.0");

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
        if (drawVelocityGizmos)
        {
            renderVelocityGizmos();
        }

        //Gizmos.DrawRay(transform.position - transform.forward * distForAddForce, transform.up);
    }

    void calcAttackAngles()
    {
        attackAngle = Vector3.Angle(transform.forward, rb.velocity);

        left_right_attack_angle = Vector3.ProjectOnPlane(rb.velocity, leftRight.transform.forward); // gameObject.transform.forward // rb.velocity.normalized
        left_right_attack_angle_magnitude = left_right_attack_angle.magnitude;

        up_down_attack_angle = Vector3.ProjectOnPlane(rb.velocity, upDown.transform.forward); // gameObject.transform.forward // rb.velocity.normalized
        up_down_attack_angle_magnitude = up_down_attack_angle.magnitude;
    }

    void renderTransformGizmos()
    {
        //float gizmosSize = 5;

        //Gizmos.color = Color.blue;
        //Gizmos.DrawRay(transform.position, transform.forward * gizmosSize);

        //Gizmos.color = Color.red;
        //Gizmos.DrawRay(transform.position, transform.right * gizmosSize);

        //Gizmos.color = Color.green;
        //Gizmos.DrawRay(transform.position, transform.up * gizmosSize);
    }

    void renderVelocityGizmos()
    {
        if (!rb) return;

        float gizmosSize = 1;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position /*- transform.forward * distForAddForce*/, velocity * gizmosSize);

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

        if (Input.GetMouseButton(1)) return;

        if (Input.GetKey(KeyCode.Q))
            moveLeft = true;
        if (Input.GetKey(KeyCode.E))
            moveRight = true;
        if (Input.GetKey(KeyCode.W))
            moveDown = true;
        if (Input.GetKey(KeyCode.S))
            moveUp = true;
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

    void rotateControlPlane() // not korrect rotate doter planes ? - add empty game object
    {
        float maxAngle = 45;
        float rotationSpeed = 1f;

        if (moveRight)
        {
            leftRight.transform.Rotate(Vector3.up, -rotationSpeed);
            if (Vector3.Angle(transform.forward, leftRight.transform.forward) > maxAngle)
                leftRight.transform.localRotation = Quaternion.Euler(0, -maxAngle, 0);
        }
        if (moveLeft)
        {
            leftRight.transform.Rotate(Vector3.up, rotationSpeed);
            if (Vector3.Angle(transform.forward, leftRight.transform.forward) > maxAngle)
                leftRight.transform.localRotation = Quaternion.Euler(0, maxAngle, 0);
        }
        if (moveUp)
        {
            upDown.transform.Rotate(Vector3.right, rotationSpeed);
            if (Vector3.Angle(transform.forward, upDown.transform.forward) > maxAngle)
                upDown.transform.localRotation = Quaternion.Euler(maxAngle, 0, 0);
        }
        if (moveDown)
        {
            upDown.transform.Rotate(Vector3.right, -rotationSpeed);
            if (Vector3.Angle(transform.forward, upDown.transform.forward) > maxAngle)
                upDown.transform.localRotation = Quaternion.Euler(-maxAngle, 0, 0);
        }

        left_right_angle = leftRight.transform.rotation.eulerAngles;
        up_down_angle = upDown.transform.rotation.eulerAngles;
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
        controlPlaneDrag = velocity.magnitude * controlPlaneDragCoeff; // * velocity
  
        Vector3 positionForAddForce = transform.position - transform.forward * distForAddForce;

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
        Vector3 positionForAddForceLeftEleron = transform.position - transform.right * 1.596f;
        Vector3 positionForAddForceRightEleron = transform.position + transform.right * 1.596f;

        eleronsDrag = velocity.magnitude * velocity.magnitude * eleronsDragCoeff; // * velocity * angle

        if (leftRoll)
        {
            rb.AddForceAtPosition(-transform.up * eleronsDrag, positionForAddForceLeftEleron);
            rb.AddForceAtPosition(transform.up * eleronsDrag, positionForAddForceRightEleron);
        }
        if (rightRoll)
        {
            rb.AddForceAtPosition(transform.up * eleronsDrag, positionForAddForceLeftEleron);
            rb.AddForceAtPosition(-transform.up * eleronsDrag, positionForAddForceRightEleron);
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

    void drawControlPlaneForces()
    {
        Vector3 positionDrawForce = transform.position - transform.forward * distForAddForce;
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
        Debug.DrawLine(positionDrawForce, positionDrawForce + forceUpDown, Color.magenta);

        Vector3 forceLeftRight = Vector3.Reflect(velocity, leftRight.transform.right) - velocity;
        Debug.DrawLine(positionDrawForce, positionDrawForce + forceLeftRight, Color.yellow);
    }

    void drawEleuronForces()
    {
        Vector3 positionDrawForce = transform.position - transform.forward * distForAddForce;
        Vector3 end;

        Vector3 positionForDrawForceLeftEleron = transform.position - transform.right * 1.596f;
        Vector3 positionForDrawForceRightEleron = transform.position + transform.right * 1.596f;

        if (leftRoll)
        {
            end = positionForDrawForceLeftEleron + transform.up * 1.5f;
            Debug.DrawLine(positionForDrawForceLeftEleron, end, new Color(240, 94, 35));

            end = positionForDrawForceRightEleron - transform.up * 1.5f;
            Debug.DrawLine(positionForDrawForceRightEleron, end, new Color(240, 94, 35));
        }
        if (rightRoll)
        {
            end = positionForDrawForceLeftEleron - transform.up * 1.5f;
            Debug.DrawLine(positionForDrawForceLeftEleron, end, new Color(240, 94, 35));

            end = positionForDrawForceRightEleron + transform.up * 1.5f;
            Debug.DrawLine(positionForDrawForceRightEleron, end, new Color(240, 94, 35));
        }
    }

    void drawCorpusForces()
    {
        Vector3 forceUpDownPlane = upDownDragCoeff * (Vector3.Reflect(velocity, transform.up) - velocity);
        Debug.DrawLine(transform.position, transform.position + forceUpDownPlane, Color.magenta);

        Vector3 forceLeftRightPlane = leftRightDragCoeff * (Vector3.Reflect(velocity, transform.right) - velocity);
        Debug.DrawLine(transform.position, transform.position + forceLeftRightPlane, Color.yellow);

        Vector3 forceForwardBackPlane = forwardDragCoeff * (Vector3.Reflect(velocity, transform.forward) - velocity);
        Debug.DrawLine(transform.position, transform.position + forceForwardBackPlane, Color.cyan);
    }

    void drawWingForces()
    {
        Vector3 forceUpDownPlane = upDownWingDragCoeff * (Vector3.Reflect(velocity, transform.up) - velocity);
        Debug.DrawLine(transform.position, transform.position + forceUpDownPlane, Color.magenta);

        Vector3 forceLeftRightPlane = leftRightWingDragCoeff * (Vector3.Reflect(velocity, transform.right) - velocity);
        Debug.DrawLine(transform.position, transform.position + forceLeftRightPlane, Color.yellow);

        Vector3 forceForwardBackPlane = forwardWingDragCoeff * (Vector3.Reflect(velocity, transform.forward) - velocity);
        Debug.DrawLine(transform.position, transform.position + forceForwardBackPlane, Color.yellow);
    }

    void calcAcceleration()
    {
        actualAcceleration = (rb.velocity - prevVelocity) / Time.fixedDeltaTime;
        prevVelocity = rb.velocity;
    }
}
