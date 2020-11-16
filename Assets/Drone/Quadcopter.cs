using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Engine
{
    private float turnovers; // in percent
    private float power;
    private float trust;
    private float engineThrustCoeff;
    private float turnCoeffByPower;
    private bool rotationDirection;
    public float engineResponse; // на сколько быстро двигатель набирает нужные обороты

    public Engine(float engineThrustCoeff, float turnCoeffByPower, bool rotationDirection, float engineResponse)
    {
        this.engineThrustCoeff = engineThrustCoeff;
        this.turnCoeffByPower = turnCoeffByPower;
        this.rotationDirection = rotationDirection;
        this.engineResponse = engineResponse;
    }

    public float Power
    {
        set
        {
            if (value >= 100)
                power = 100f;
            else if (value <= 0)
                power = 0f;
            else
                power = value;
        }
        get
        {
            return power;
        }
    }

    public float Thrust // тяга
    {
        get
        {
            return turnovers * engineThrustCoeff;
        }
    }

    public float Turnovers // обороты
    {
        get
        {
            return turnovers;
        }
    }

    public float Torque // крутяший момент
    {
        get
        {
            if (rotationDirection)
            {
                return power * turnCoeffByPower;
            } else
            {
                return -power * turnCoeffByPower;
            }
        }
    }

    public void Update()
    {
        if (Mathf.Abs(power - turnovers) <= engineResponse)
            turnovers = power;
        else if (power - turnovers > 0)
            turnovers += engineResponse;
        else
            turnovers -= engineResponse;

        //turnovers = power;
    }
}

public class Quadcopter : MonoBehaviour
{
    public float engineThrustCoeff;
    public float engineTorqueCoeff;
    public float engineResponse;

    public Rigidbody rb;

    private Engine leftBackEngine;
    private Engine leftFwdEngine;
    private Engine rightBackEngine;
    private Engine rightFwdEngine;

    public float leftBackEngineThrust;
    public float leftFwdEngineThrust;
    public float rightBackEngineThrust;
    public float rightFwdEngineThrust;

    public float upDownCorpusDragCoeff;
    public float leftRightCorpusDragCoeff;
    public float fwdBackCorpusDragCoeff;

    public float turnCoeffByPower; // зависсимость крутящего момента двигателя от ег мощности
    public float turnCoeffPower; //на сколько нужно имменить мощность двигателя чтоб дрон поворачивался

    public Vector3 spd;
    public float PDResult;

    public float pCoeffHoldAltitude;
    public float dCoeffHoldAltitude;

    public float aCoeffHoldAngle;
    public float pCoeffHoldAngle;
    public float dCoeffHoldAngle;
    public float iCoeffHoldAngle;
    public float maxSumForPIDController;

    public float maxAngle;

    public Vector3 localAngularVelocity;

    Shared.PIDController holdAnglePIDController;

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
        rb.inertiaTensor = tensor1 * rb.mass;

        leftBackEngine =  new Engine(engineThrustCoeff, turnCoeffByPower, false, engineResponse);
        leftFwdEngine =   new Engine(engineThrustCoeff, turnCoeffByPower, true, engineResponse);
        rightBackEngine = new Engine(engineThrustCoeff, turnCoeffByPower, true, engineResponse);
        rightFwdEngine =  new Engine(engineThrustCoeff, turnCoeffByPower, false, engineResponse);

        holdAnglePIDController = new Shared.PIDController(aCoeffHoldAngle, pCoeffHoldAngle, dCoeffHoldAngle, iCoeffHoldAngle, maxSumForPIDController);

        //rb.AddForceAtPosition(new Vector3(100,0,0), new Vector3(0,0,1));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rb.inertiaTensor = tensor1 * rb.mass;

        localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity) * Mathf.Rad2Deg; // deg / sec

        //holdAltitude(5);

        // call before addForceEngines
        //turnDrone();
        //moveDroneByDirection();
        holdAngle();

        addForceLBEngine();
        addForceLFEngine();
        addForceRBEngine();
        addForceRFEngine();

        addCorpusForces();

        spd = rb.velocity;

        holdAnglePIDController.Update(aCoeffHoldAngle, pCoeffHoldAngle, dCoeffHoldAngle, iCoeffHoldAngle, maxSumForPIDController);
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

        float powerCoeff = 2;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            leftBackEngine.Power += powerCoeff;
            rightBackEngine.Power += powerCoeff;
            leftFwdEngine.Power += powerCoeff;
            rightFwdEngine.Power += powerCoeff;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            leftBackEngine.Power -= powerCoeff;
            rightBackEngine.Power -= powerCoeff;
            leftFwdEngine.Power -= powerCoeff;
            rightFwdEngine.Power -= powerCoeff;
        }
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

    void addEngineThrustForces(Engine engine, Vector3 positionForAddForce) //turnovers in percent
    {
        engine.Update();

        Vector3 force = engine.Thrust * transform.up;

        rb.AddForceAtPosition(force, positionForAddForce);

        rb.AddTorque(transform.up * engine.Torque);

        Vector3 shift = new Vector3(0.02f, 0.02f, 0.02f);
        UnityEngine.Debug.DrawLine(positionForAddForce + shift, positionForAddForce + shift + engine.Turnovers * transform.up * 0.2f, Color.blue); // Turnovers
        UnityEngine.Debug.DrawLine(positionForAddForce - shift, positionForAddForce - shift + engine.Power * transform.up * 0.2f, Color.green); // Power

        //UnityEngine.Debug.DrawLine(positionForAddForce, positionForAddForce + force * 0.01f, Color.magenta); // force

        leftBackEngineThrust = leftBackEngine.Thrust;
        leftFwdEngineThrust =  leftFwdEngine.Thrust;
        rightBackEngineThrust = rightBackEngine.Thrust;
        rightFwdEngineThrust = rightFwdEngine.Thrust;
}

    void addForceLBEngine()
    {
        Vector3 pos = transform.position + 1.2f * -transform.forward + 0.7f * -transform.right;
        // calc power leftBackEnginePower = ...
        addEngineThrustForces(leftBackEngine, pos);
    }

    void addForceLFEngine()
    {
        Vector3 pos = transform.position + -1.2f * -transform.forward + 0.7f * -transform.right;
        // calc power leftFwdEnginePower = ...
        addEngineThrustForces(leftFwdEngine, pos);
    }

    void addForceRBEngine()
    {
        Vector3 pos = transform.position + 1.2f * -transform.forward - 0.7f * -transform.right;
        // calc power rightBackEnginePower = ...
        addEngineThrustForces(rightBackEngine, pos);
    }

    void addForceRFEngine()
    {
        Vector3 pos = transform.position + -1.2f * -transform.forward - 0.7f * -transform.right;
        // calc power rightFwdEnginePower = ...
        addEngineThrustForces(rightFwdEngine, pos);
    }

    void holdAltitude(float altitude)
    {
        PDResult = Shared.PDController(altitude - transform.position.y, -rb.velocity.y, pCoeffHoldAltitude, dCoeffHoldAltitude);
        float power = Mathf.Clamp(PDResult, 0, 100);
        leftBackEngine.Power = power;
        leftFwdEngine.Power = power;
        rightBackEngine.Power = power;
        rightFwdEngine.Power = power;
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

    void turnLeft()
    {
        leftBackEngine.Power += turnCoeffPower;
        rightFwdEngine.Power += turnCoeffPower;
        leftFwdEngine.Power -= turnCoeffPower;
        rightBackEngine.Power -= turnCoeffPower;
    }

    void turnRight()
    {
        leftBackEngine.Power -= turnCoeffPower;
        rightFwdEngine.Power -= turnCoeffPower;
        leftFwdEngine.Power += turnCoeffPower;
        rightBackEngine.Power += turnCoeffPower;
    }

    void moveDroneLeft()
    {
        leftBackEngine.Power -= 2;
        rightFwdEngine.Power += 2;
        leftFwdEngine.Power -= 2;
        rightBackEngine.Power += 2;
    }

    void moveDroneRight()
    {
        leftBackEngine.Power += 2;
        rightFwdEngine.Power -= 2;
        leftFwdEngine.Power += 2;
        rightBackEngine.Power -= 2;
    }

    void moveDroneFwd()
    {
        leftBackEngine.Power += 2;
        rightFwdEngine.Power -= 2;
        leftFwdEngine.Power -= 2;
        rightBackEngine.Power += 2;
    }

    void moveDroneBack()
    {
        leftBackEngine.Power -= 2;
        rightFwdEngine.Power += 2;
        leftFwdEngine.Power += 2;
        rightBackEngine.Power -= 2;
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
        float PDResultFwdBack = holdAnglePIDController.Calculate(deltaAngleFwdback, -localAngularVelocity.x);
        float powerFwdBack = Mathf.Clamp(PDResultFwdBack, -100, 100);
        leftBackEngine.Power += powerFwdBack;
        leftFwdEngine.Power -= powerFwdBack;
        rightBackEngine.Power += powerFwdBack;
        rightFwdEngine.Power -= powerFwdBack;

        //float deltaAngleLeftRight = -Mathf.DeltaAngle(0, transform.localEulerAngles.z);
        //float PDResultLeftRight = holdAnglePIDController.Calculate(deltaAngleLeftRight, -localAngularVelocity.z);
        //float powerLeftRight = Mathf.Clamp(PDResultLeftRight, -100, 100);
        //leftBackEngine.Power -= powerLeftRight;
        //leftFwdEngine.Power -= powerLeftRight;
        //rightBackEngine.Power += powerLeftRight;
        //rightFwdEngine.Power += powerLeftRight;
    }
}
