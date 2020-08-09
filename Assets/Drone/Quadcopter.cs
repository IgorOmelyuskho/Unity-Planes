using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quadcopter : MonoBehaviour
{
    public float engineThrustCoeff;
    public float engineTorqueCoeff;

    public Rigidbody rb;

    public float leftBackEnginePower;
    public float leftFwdEnginePower;
    public float rightBackEnginePower;
    public float rightFwdEnginePower;

    public float upDownCorpusDragCoeff;
    public float leftRightCorpusDragCoeff;
    public float fwdBackCorpusDragCoeff;

    public float turnCoeff;

    public Vector3 spd;
    public float PDResult;

    public float pCoeffHoldAltitude;
    public float dCoeffHoldAltitude;

    public float pCoeffHoldAngle;
    public float dCoeffHoldAngle;

    public float maxAngle;

    public Vector3 localAngularVelocity;

    bool moveLeft;
    bool moveRight;
    bool moveFwd;
    bool moveBack;
    bool leftRoll;
    bool rightRoll;

    public Vector3 tensor1; // todo values

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        print(tensor1);
        print(rb.mass);
        rb.inertiaTensor = tensor1 * rb.mass;

        //rb.AddForceAtPosition(new Vector3(100,0,0), new Vector3(0,0,1));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rb.inertiaTensor = tensor1 * rb.mass;

        localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity) * Mathf.Rad2Deg; // deg / sec

        //holdAltitude(5);

        // call before addForceEngines
        turnDrone();
        //moveDroneByDirection();
        holdAngle();

        clampEnginesPower();

        addForceLBEngine();
        addForceLFEngine();
        addForceRBEngine();
        addForceRFEngine();

        addCorpusForces();

        spd = rb.velocity;
    }

    void Update()
    {
        handleKeyInput();
    }

    void handleKeyInput()
    {
        moveLeft = false;
        moveRight = false;
        moveFwd = false;
        moveBack = false;
        leftRoll = false;
        rightRoll = false;

        if (Input.GetKey(KeyCode.Q))
            leftRoll = true;

        if (Input.GetKey(KeyCode.E))
            rightRoll = true;

        if (Input.GetKey(KeyCode.W))
            moveFwd = true;

        if (Input.GetKey(KeyCode.S))
            moveBack = true;

        if (Input.GetKey(KeyCode.A))
            moveLeft = true;

        if (Input.GetKey(KeyCode.D))
            moveRight = true;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            leftBackEnginePower += 2;
            rightBackEnginePower += 2;
            leftFwdEnginePower += 2;
            rightFwdEnginePower += 2;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            leftBackEnginePower -= 2;
            rightBackEnginePower -= 2;
            leftFwdEnginePower -= 2;
            rightFwdEnginePower -= 2;
        }

        clampEnginesPower();
    }

    void addCorpusForces()
    {
        float upDownDrag = rb.velocity.magnitude * upDownCorpusDragCoeff;
        Vector3 forceUpDownPlane = upDownDrag * (Vector3.Reflect(rb.velocity, transform.up) - rb.velocity);
        rb.AddForceAtPosition(forceUpDownPlane, transform.position);

        float leftRightnDrag = rb.velocity.magnitude * leftRightCorpusDragCoeff;
        Vector3 forceLeftRightPlane = leftRightnDrag * (Vector3.Reflect(rb.velocity, transform.right) - rb.velocity);
        rb.AddForceAtPosition(forceLeftRightPlane, transform.position);

        float fwdBackCorpusDrag = rb.velocity.magnitude * fwdBackCorpusDragCoeff;
        Vector3 forceForwardBackPlane = fwdBackCorpusDrag * (Vector3.Reflect(rb.velocity, transform.forward) - rb.velocity);
        rb.AddForceAtPosition(forceForwardBackPlane, transform.position);
    }

    void addEngineThrustForce(float power, Vector3 positionForAddForce)
    {
        float engineThrust = engineThrustCoeff * power;

        Vector3 force = engineThrust * transform.up;

        rb.AddForceAtPosition(force, positionForAddForce);

        UnityEngine.Debug.DrawLine(positionForAddForce, positionForAddForce + force * 0.01f, Color.magenta);
    }


    void addEngineTorque(float power)
    {
        float engineTorque = engineTorqueCoeff * power;
        rb.AddTorque(transform.up * engineTorque);
    }

    void addForceLBEngine()
    {
        Vector3 pos = transform.position + 1.2f * -transform.forward + 0.7f * -transform.right;
        // calc power leftBackEnginePower = ...
        addEngineThrustForce(leftBackEnginePower, pos);

        addEngineTorque(leftBackEnginePower);
    }

    void addForceLFEngine()
    {
        Vector3 pos = transform.position + -1.2f * -transform.forward + 0.7f * -transform.right;
        // calc power leftFwdEnginePower = ...
        addEngineThrustForce(leftFwdEnginePower, pos);

        addEngineTorque(-leftFwdEnginePower); //
    }

    void addForceRBEngine()
    {
        Vector3 pos = transform.position + 1.2f * -transform.forward - 0.7f * -transform.right;
        // calc power rightBackEnginePower = ...
        addEngineThrustForce(rightBackEnginePower, pos);

        addEngineTorque(-rightBackEnginePower); //
    }

    void addForceRFEngine()
    {
        Vector3 pos = transform.position + -1.2f * -transform.forward - 0.7f * -transform.right;
        // calc power rightFwdEnginePower = ...
        addEngineThrustForce(rightFwdEnginePower, pos);

        addEngineTorque(rightFwdEnginePower);
    }

    void holdAltitude(float altitude)
    {
        PDResult = Shared.PDController(altitude - transform.position.y, -rb.velocity.y, pCoeffHoldAltitude, dCoeffHoldAltitude);
        float power = Mathf.Clamp(PDResult, 0, 100);
        leftBackEnginePower = power;
        leftFwdEnginePower = power;
        rightBackEnginePower = power;
        rightFwdEnginePower = power;
    }

    void holdAngle()
    {
        if (moveLeft || moveRight)
            holdAngleLeftRight();
        else if (moveFwd || moveBack)
            holdAngleFwdBack();
        else holdAngleZero();
    }

    void turnDrone()
    {
        if (leftRoll)
            turnLeft();

        if (rightRoll)
            turnRight();
    }

    void moveDroneByDirection()
    {
        if (moveLeft)
            moveDroneLeft();

        if (moveRight)
            moveDroneRight();

        if (moveFwd)
            moveDroneFwd();

        if (moveBack)
            moveDroneBack();
    }

    void clampEnginesPower()
    {
        leftBackEnginePower = Mathf.Clamp(leftBackEnginePower, 0, 100);
        leftFwdEnginePower = Mathf.Clamp(leftFwdEnginePower, 0, 100);
        rightBackEnginePower = Mathf.Clamp(rightBackEnginePower, 0, 100);
        rightFwdEnginePower = Mathf.Clamp(rightFwdEnginePower, 0, 100);
    }

    void turnLeft()
    {
        leftBackEnginePower += turnCoeff;
        rightFwdEnginePower += turnCoeff;
        leftFwdEnginePower -= turnCoeff;
        rightBackEnginePower -= turnCoeff;
    }

    void turnRight()
    {
        leftBackEnginePower -= turnCoeff;
        rightFwdEnginePower -= turnCoeff;
        leftFwdEnginePower += turnCoeff;
        rightBackEnginePower += turnCoeff;
    }

    void moveDroneLeft()
    {
        leftBackEnginePower -= 2;
        rightFwdEnginePower += 2;
        leftFwdEnginePower -= 2;
        rightBackEnginePower += 2;
    }

    void moveDroneRight()
    {
        leftBackEnginePower += 2;
        rightFwdEnginePower -= 2;
        leftFwdEnginePower += 2;
        rightBackEnginePower -= 2;
    }

    void moveDroneFwd()
    {
        leftBackEnginePower += 2;
        rightFwdEnginePower -= 2;
        leftFwdEnginePower -= 2;
        rightBackEnginePower += 2;
    }

    void moveDroneBack()
    {
        leftBackEnginePower -= 2;
        rightFwdEnginePower += 2;
        leftFwdEnginePower += 2;
        rightBackEnginePower -= 2;
    }

    void holdAngleLeftRight()
    {

    }

    void holdAngleFwdBack()
    {
        //deltaAFwd = -Mathf.DeltaAngle(maxAngle, transform.localEulerAngles.x);

        //float PDResult = Shared.PDController(deltaAFwd, localAngularVelocity.x, pCoeffHoldAngle, dCoeffHoldAngle);
        //float power = Mathf.Clamp(PDResult, 0, 100);
        //if (deltaAFwd < 0)
        //    power = -power;
        //leftBackEnginePower += power;
        //leftFwdEnginePower -= power;
        //rightBackEnginePower += power;
        //rightFwdEnginePower -= power;
    }

    void holdAngleZero()
    {
        float deltaAngleFwdback = -Mathf.DeltaAngle(0, transform.localEulerAngles.x);
        float PDResultFwdBack = Shared.PDController(deltaAngleFwdback, -localAngularVelocity.x, pCoeffHoldAngle, dCoeffHoldAngle);
        float powerFwdBack = Mathf.Clamp(PDResultFwdBack, -100, 100);
        leftBackEnginePower += powerFwdBack;
        leftFwdEnginePower -= powerFwdBack;
        rightBackEnginePower += powerFwdBack;
        rightFwdEnginePower -= powerFwdBack;

        float deltaAngleLeftRight = -Mathf.DeltaAngle(0, transform.localEulerAngles.z);
        float PDResultLeftRight = Shared.PDController(deltaAngleLeftRight, -localAngularVelocity.z, pCoeffHoldAngle, dCoeffHoldAngle);
        float powerLeftRight = Mathf.Clamp(PDResultLeftRight, -100, 100);
        leftBackEnginePower -= powerLeftRight;
        leftFwdEnginePower -= powerLeftRight;
        rightBackEnginePower += powerLeftRight;
        rightFwdEnginePower += powerLeftRight;
    }
}
