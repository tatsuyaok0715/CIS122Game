using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnityEngine;

public class BulletPhysics : MonoBehaviour
{
    [Header("Mass Grains")]
    private float mass = 150f;

    [Header("Muzzle velocity m/s")]
    private float MuzzleVelocity = 880f;

    [Header("Twist Inch/Turn")]
    private float Twist = 10f;

    [Header("Bullet Diameter m")]
    private float bulletDia = 0.0078232f;

    [Header("Bullet Diameter m")]
    private float bulletLength = 0.0338328f;

    [Header("Pressure pa")]
    private float pressure = 101325f;

    [Header("Temperature k")]
    private float temp = 288.16f;

    private Rigidbody m_Rigidbody;

    private Vector3 m_pos;
    private Vector3 m_prevPos;
    private float m_velocity;

    private Bullet m_bullet;

    private bool fireBullet = false;
    private bool bulletInflight = false;
    private float firingAngle = 177;
    private Vector3 barrel_Pos;
    private Vector3 barrel_Rotation;
    private Vector3 StartRotation;



    // Start is called before the first frame update
    void Start()
    {   

        var gObj = GameObject.Find("muzzle_Ref");

        if (gObj){

            

            barrel_Pos = gObj.transform.position;
            StartRotation = gObj.transform.eulerAngles;

            m_Rigidbody = GetComponent<Rigidbody>();
            m_bullet = new Bullet(mass, MuzzleVelocity, Twist, bulletDia, bulletLength,pressure, temp ,barrel_Pos,StartRotation);
            m_bullet.fireBullet();
            bulletInflight = true;
        }

        Debug.Log("Pos = "+ gObj.transform.parent.position);
        Debug.Log("Rot = "+ gObj.transform.parent.eulerAngles);


    }


    // Update is called once per frame
    void Update()
    {   

        
        m_prevPos = m_Rigidbody.position;
        m_velocity = m_bullet.m_vel;
        m_bullet.update();
        m_Rigidbody.position = m_bullet.m_Position;
        m_pos = m_bullet.m_Position;
        

    }



}
    

public class Bullet
{

    //G1 – G1 projectiles are flatbase bullets with 2 caliber nose ogive and are the most common type of bullet.
    //G2 – bullets in the G2 range are Aberdeen J projectiles
    //G5 – G5 bullets are short 7.5 degree boat-tails, with 6.19 caliber long tangent ogive
    //G6 – G6 are flatbase bullets with a 6 cailber secant ogive
    //G7 – Bullets with the G7 BC are long 7.5 degree boat-tails, with 10 caliber tangent ogive, and are very popular with manufacturers for extremely low-drag bullets.
    //G8
    //GS

    // Type Def
    public enum dragModel { G1, G2, G3, G4, G5, G6, G7, G8, GS };
    public dragModel drag_Model;

    float bulletLifeTime = 30f; // Seconds
    float timeOfFlight = 0f;

    // Vectors
    float[] start_Pos = new float[3];
    float[] pos = new float[3];
    float[] prev_pos = new float[3];
    float[] spin = new float[3];
    float[] lateralAccel = new float[3];
    float[] drag_Vector = new float[3];
    float[] velocity_Vector = new float[3];
    float[] centripetalAccel_Vector = new float[3];
    float[] centripetal_Vector = new float[3];
    float[] coriolis_Vector = new float[3];
    float[] coriolisAccel_Vector = new float[3];
    float[] prev_coriolis = new float[3];
    float[] wind_Vector = new float[3];
    float[] windForce_Vector = new float[3];
    float[] gravity_Vector = new float[3];
    float[] velocity_Vector_dt = new float[3];
    float[] spinDrift_Vector = new float[3];
    float[] spinDriftAccel_Vector = new float[3];

    // Metric
    private float Re = 6356766f;
    private float Me = 5.9722e24f;
    private float g = -9.80665f;
    private float k_omega = 0.000072921159f;
    private float mass;
    private float mass_ibs;
    private float twist;
    private float muzzle_Velocity;
    private float bullet_Dia;
    private float bullet_Len;
    private float baro;
    private float pressure;
    private float temp_k;
    private float front_Area;
    private float dt;
    private float bullet_Direction;
    private float elapsed_Ms;
    private float cd_Current;
    private float currentLatitude;
    private float spinDriftMag;


    //Imperial 
    private float muzzle_Vel_Fps;
    private float grains;
    private float calibers;
    private float bullet_Dia_Inch;
    private float bullet_Len_Inch;
    private float temp_F;
    private float twist_Calibers;
    private float ballistic_Coefficient;
    private float sectional_Density;
    private float stability_Fac;

    public bool projectile_Despawn { get; set; } 
    public Vector3 m_Position {get; set;}
    public Vector3 StartRotation;
    public float m_vel {get; set;}
    private Vector3 StartVac;
    public GameObject muzzleRef;


    public Bullet( float grains, float muzzleVelocity, float barrelTwist, float bulletDia, float bulletLen, float pressure, float temp, Vector3 initialPos, Vector3 InitRotation)
    {

        this.drag_Model = dragModel.G7;
        this.mass = grains * 0.0000647989f;
        this.mass_ibs = mass * 2.20462f;
        this.twist = barrelTwist;
        this.bullet_Dia = bulletDia;
        this.bullet_Len = bulletLen;
        this.temp_k = temp;
        this.muzzle_Velocity = muzzleVelocity;
        this.pressure = pressure;
        this.grains = this.mass * 15432.4f;
        this.bullet_Dia_Inch = this.bullet_Dia * 39.3701f;
        this.bullet_Len_Inch = this.bullet_Len * 39.3701f;
        this.calibers = this.bullet_Len / this.bullet_Dia;
        this.twist_Calibers = barrelTwist / this.bullet_Dia_Inch;
        this.front_Area = Mathf.PI * Mathf.Pow(this.bullet_Dia / 2f, 2);
        this.sectional_Density = this.mass_ibs / Mathf.Pow(this.bullet_Dia_Inch, 2);
        this.dt = Time.fixedDeltaTime; // Fixed Delta Time (Seconds)
        this.elapsed_Ms = 0f; 
        this.spinDriftMag = 0;
        this.StartVac = initialPos;
        this.StartRotation = InitRotation;

        this.stability_Fac = GetStabilityFactor();

    }

    public void fireBullet()
    {

        // Calculate Vector Component

        this.velocity_Vector[0] =  this.muzzle_Velocity * Mathf.Cos(( (StartRotation.y ) * Mathf.PI / 180 ));
        this.velocity_Vector[1] = -this.muzzle_Velocity * Mathf.Sin(( (StartRotation.y ) * Mathf.PI / 180 )) * Mathf.Sin(( (StartRotation.z  ) * Mathf.PI / 180 ));
        this.velocity_Vector[2] =  this.muzzle_Velocity * Mathf.Sin(( (StartRotation.y ) * Mathf.PI / 180 )) * Mathf.Cos(( (StartRotation.z  ) * Mathf.PI / 180 ));


        //Debug.Log(" X = " + StartRotation.x + " Vel = " + this.velocity_Vector[0]);
        //Debug.Log(" Y = " + StartRotation.y + " Vel = " + this.velocity_Vector[1]);
        //Debug.Log(" Z = " + StartRotation.z + " Vel = " + this.velocity_Vector[2]);
    

        this.pos[0] = StartVac.x;
        this.pos[1] = StartVac.y;
        this.pos[2] = StartVac.z;


        velocity_Vector_dt[0] = this.velocity_Vector[0] * dt;
        velocity_Vector_dt[1] = this.velocity_Vector[1] * dt;
        velocity_Vector_dt[2] = 0;

        projectile_Despawn = false;

        //Console.WriteLine("Velocity x = " + velocity_Vector[0] + " y = " + velocity_Vector[1] + " z = " + velocity_Vector[2]);

    }


    public void update()
    {

        this.timeOfFlight = this.timeOfFlight + this.dt;

        if (this.timeOfFlight >  this.bulletLifeTime )
        {
            projectile_Despawn = true;
        }

        // If Bullet Collide with the Ground
        if (pos[1] < 0)
        {
            projectile_Despawn = true;
        }

        getGravity();
        getDrag();
        getCoriolis();
        getWindForce();
        getCentripetal();
        getSpinDrift();

        integratePosition();

        //Update Unity PositionVector
        m_Position = new Vector3(pos[0], pos[1], pos[2]);
        m_vel = getRelativeSpeed();
    }

    public void integratePosition()
    {

        // Integrate Gravity
        this.velocity_Vector[0] = this.velocity_Vector[0] + this.gravity_Vector[0];
        this.velocity_Vector[1] = this.velocity_Vector[1] + this.gravity_Vector[1];
        this.velocity_Vector[2] = this.velocity_Vector[2] + this.gravity_Vector[2];

        // Integrate Drag
        this.velocity_Vector[0] = this.velocity_Vector[0] - this.drag_Vector[0];
        this.velocity_Vector[1] = this.velocity_Vector[1] - this.drag_Vector[1];
        this.velocity_Vector[2] = this.velocity_Vector[2] - this.drag_Vector[2];

        // Integrate Wind
        this.velocity_Vector[0] = this.velocity_Vector[0] + this.windForce_Vector[0];
        this.velocity_Vector[1] = this.velocity_Vector[1] + this.windForce_Vector[1];
        this.velocity_Vector[2] = this.velocity_Vector[2] + this.windForce_Vector[2];

        // Integrate Coriolis
        this.velocity_Vector[0] = this.velocity_Vector[0] + this.coriolisAccel_Vector[0];
        this.velocity_Vector[1] = this.velocity_Vector[1] + this.coriolisAccel_Vector[1];
        this.velocity_Vector[2] = this.velocity_Vector[2] + this.coriolisAccel_Vector[2];

        // Integrate Centripetal
        this.velocity_Vector[1] = this.velocity_Vector[1] + this.centripetalAccel_Vector[1];

        // Integrate Spin Drift
        this.velocity_Vector[0] = this.velocity_Vector[0] + this.spinDriftAccel_Vector[0];
        this.velocity_Vector[1] = this.velocity_Vector[1] + this.spinDriftAccel_Vector[1];
        this.velocity_Vector[2] = this.velocity_Vector[2] + this.spinDriftAccel_Vector[2];

        // Integrate Velocity & Update Displacement
        this.prev_pos[0] = pos[0];
        this.prev_pos[1] = pos[1];
        this.prev_pos[2] = pos[2];

        this.pos[0] = pos[0] + (this.velocity_Vector[0] * dt);
        this.pos[1] = pos[1] + (this.velocity_Vector[1] * dt);
        this.pos[2] = pos[2] + (this.velocity_Vector[2] * dt);


    }

    private void getSpinDrift()
    {

        if (this.spinDriftMag == 0)
        {
            this.spinDriftMag = 1.25f * (GetStabilityFactor() + 1.2f) * Mathf.Pow(dt, 1.83f);
            this.spinDriftAccel_Vector[0] = this.spinDriftMag;
            this.spinDriftAccel_Vector[1] = 0;
            this.spinDriftAccel_Vector[2] = 0;
        }
    }

    private void getLat()
    {
        this.currentLatitude = 44.166130f;
    }

    private void getCentripetal()
    {
        // Eötvös effect . Vertical Coroiolis Drift. 
        // Reference : http://www.cleonis.nl/physics/phys256/eotvos.php
        // x = east west vector
        // z = north south vector 

        this.centripetalAccel_Vector[0] = 0;
        this.centripetalAccel_Vector[2] = 0;

        this.centripetalAccel_Vector[1] = (2 * k_omega * this.velocity_Vector[0] * Mathf.Sin(this.currentLatitude * (Mathf.PI / 180)))
                                           + ((Mathf.Pow(velocity_Vector[0], 2) + Mathf.Pow(velocity_Vector[2], 2)) / this.Re);

        // Integrate To Velocity
        this.centripetal_Vector[0] = 0;
        this.centripetal_Vector[2] = 0;
        this.centripetal_Vector[1] = this.centripetal_Vector[1] + (this.centripetalAccel_Vector[1] * dt);

        //Console.WriteLine("Centripetal X = " + this.centripetal_Vector[0] + " Y = " + this.centripetal_Vector[1] + " Z = " + this.centripetal_Vector[2]);

    }

    private void getCoriolis()
    {

        // Reference from https://phas.ubc.ca/~berciu/TEACHING/PHYS206/LECTURES/FILES/coriolis.pdf
        getLat();

        // Coriolis acceleration with respect to dt
        this.coriolisAccel_Vector[0] = velocity_Vector[1] * k_omega * Mathf.Sin(currentLatitude * (Mathf.PI / 180)) * Mathf.Pow(dt, 2);
        this.coriolisAccel_Vector[2] = velocity_Vector[1] * k_omega * Mathf.Cos(currentLatitude * (Mathf.PI / 180)) * Mathf.Pow(dt, 2);
        this.coriolisAccel_Vector[1] = -((velocity_Vector[0] * k_omega * Mathf.Sin(currentLatitude * (Mathf.PI / 180))) 
            + (velocity_Vector[2] * k_omega * Mathf.Cos(currentLatitude * (Mathf.PI / 180)))) * Mathf.Pow(dt, 2)
            + ((g * k_omega * Mathf.Cos(currentLatitude * (Mathf.PI / 180))) * (Mathf.Pow(dt, 3) / 3));

        // Integrate To Velocity
        this.coriolis_Vector[0] = this.coriolis_Vector[0] + this.coriolisAccel_Vector[0];
        this.coriolis_Vector[1] = this.coriolis_Vector[1] + this.coriolisAccel_Vector[1];
        this.coriolis_Vector[2] = this.coriolis_Vector[2] + this.coriolisAccel_Vector[2];

    }


    public void updateWind()
    {
        // air speed m/s
        this.wind_Vector[0] = 5f;
        this.wind_Vector[1] = 2f;
        this.wind_Vector[2] = -10f;
    }
    public void getWindForce()
    {

        updateWind();

        float air_Density = (this.pressure / ((287.05f) * this.temp_k));

        this.windForce_Vector[0] = (0.5f * air_Density * Mathf.Pow(this.wind_Vector[0], 2) * this.front_Area * this.cd_Current) / this.mass * dt;
        this.windForce_Vector[1] = (0.5f * air_Density * Mathf.Pow(this.wind_Vector[1], 2) * (this.bullet_Dia* this.bullet_Len) * this.cd_Current) / this.mass * dt;
        this.windForce_Vector[2] = (0.5f * air_Density * Mathf.Pow(this.wind_Vector[2], 2) * (this.bullet_Dia * this.bullet_Len) * this.cd_Current) / this.mass * dt;

        //Console.WriteLine("windForce_Vector x = " + windForce_Vector[0] + " y = " + windForce_Vector[1] + " z = " + windForce_Vector[2]);

    }

    public void getGravity()
    {

        // Reference from: 2.2 Ballistic Model: Empirical Data to Determine Transonic Drag Coefficient pdf

        float r = Mathf.Sqrt(Mathf.Pow(pos[0], 2) + Mathf.Pow( ((pos[1]) + this.Re), 2) );

        this.gravity_Vector[0] = 0;
        this.gravity_Vector[1] = this.g * dt;
        this.gravity_Vector[2] = 0;

    }

    public void getDrag()
    {

        float _drag = this.dt * getRetardation();

        float[] v_drag = { 0, 0, 0 };

        v_drag = vectorNormalize(getTrueSpeed());

        this.drag_Vector[0] = v_drag[0] * _drag;
        this.drag_Vector[1] = v_drag[1] * _drag;
        this.drag_Vector[2] = v_drag[2] * _drag;

    }


    private float[] getTrueSpeed()
    {

        // Get Velocity Vector3 from unity. Using rb.Velocity.Magnitude
        return vectorOperation(this.velocity_Vector, this.wind_Vector, "+");
    }

    private float getRelativeSpeed()
    {

        // Get Velocity Vector3 from unity. Using rb.Velocity.Magnitude
        return vectorlength(vectorOperation(this.velocity_Vector, this.wind_Vector, "-"));
    }

    private float getMach(float relativeSpeed, float temp_k)
    {
        float _mach;
        float _c;

        // Speed of sound (m/s)
        // C0 = 20.046 m/s @ 1k

        _c = 20.046f * Mathf.Sqrt(temp_k);
        _mach = relativeSpeed / _c;

        return _mach;
    }



    public float getRetardation()
    {
        // Bullet Drag Force based on cd and kd
        // Reference https://www.jbmballistics.com/ballistics/topics/cdkd.shtml

        float _cd = getDragCoefficient();
        this.cd_Current = _cd;
        float _kd = _cd * 0.3927f;
        float air_Density = (this.pressure / ((287.05f) * this.temp_k));
        float rel_Vel = getRelativeSpeed();
        float drag = 0;

        this.ballistic_Coefficient = this.mass_ibs / (_cd * ( Mathf.Pow( this.bullet_Dia_Inch , 2) * Mathf.PI ));
        drag = (0.5f * air_Density * Mathf.Pow(rel_Vel, 2) * this.front_Area * _cd);

        return drag / this.mass; 
    }

    public float getDragCoefficient()
    {
        // Drag Model data imported from https://www.alternatewars.com/BBOW/Ballistics/Ext/Drag_Tables.htm
        // Code generated by vba code written by Jacob Tang

        float mach;
        float cd = 0;

        mach = getMach(getRelativeSpeed(), temp_k);

        if (drag_Model == dragModel.G1)
        {

            if (mach < 0.05f) { cd = 0.2558f; }
            else if (mach < 0.1f) { cd = 0.2487f; }
            else if (mach < 0.15f) { cd = 0.2413f; }
            else if (mach < 0.2f) { cd = 0.2344f; }
            else if (mach < 0.25f) { cd = 0.2278f; }
            else if (mach < 0.3f) { cd = 0.2214f; }
            else if (mach < 0.35f) { cd = 0.2155f; }
            else if (mach < 0.4f) { cd = 0.2104f; }
            else if (mach < 0.45f) { cd = 0.2061f; }
            else if (mach < 0.5f) { cd = 0.2032f; }
            else if (mach < 0.55f) { cd = 0.202f; }
            else if (mach < 0.6f) { cd = 0.2034f; }
            else if (mach < 0.7f) { cd = 0.2165f; }
            else if (mach < 0.73f) { cd = 0.223f; }
            else if (mach < 0.75f) { cd = 0.2313f; }
            else if (mach < 0.78f) { cd = 0.2417f; }
            else if (mach < 0.8f) { cd = 0.2546f; }
            else if (mach < 0.83f) { cd = 0.2706f; }
            else if (mach < 0.85f) { cd = 0.2901f; }
            else if (mach < 0.88f) { cd = 0.3136f; }
            else if (mach < 0.9f) { cd = 0.3415f; }
            else if (mach < 0.93f) { cd = 0.3734f; }
            else if (mach < 0.95f) { cd = 0.4084f; }
            else if (mach < 0.98f) { cd = 0.4448f; }
            else if (mach < 1f) { cd = 0.4805f; }
            else if (mach < 1.03f) { cd = 0.5136f; }
            else if (mach < 1.05f) { cd = 0.5427f; }
            else if (mach < 1.08f) { cd = 0.5677f; }
            else if (mach < 1.1f) { cd = 0.5883f; }
            else if (mach < 1.13f) { cd = 0.6053f; }
            else if (mach < 1.15f) { cd = 0.6191f; }
            else if (mach < 1.2f) { cd = 0.6393f; }
            else if (mach < 1.25f) { cd = 0.6518f; }
            else if (mach < 1.3f) { cd = 0.6589f; }
            else if (mach < 1.35f) { cd = 0.6621f; }
            else if (mach < 1.4f) { cd = 0.6625f; }
            else if (mach < 1.45f) { cd = 0.6607f; }
            else if (mach < 1.5f) { cd = 0.6573f; }
            else if (mach < 1.55f) { cd = 0.6528f; }
            else if (mach < 1.6f) { cd = 0.6474f; }
            else if (mach < 1.65f) { cd = 0.6413f; }
            else if (mach < 1.7f) { cd = 0.6347f; }
            else if (mach < 1.75f) { cd = 0.628f; }
            else if (mach < 1.8f) { cd = 0.621f; }
            else if (mach < 1.85f) { cd = 0.6141f; }
            else if (mach < 1.9f) { cd = 0.6072f; }
            else if (mach < 1.95f) { cd = 0.6003f; }
            else if (mach < 2f) { cd = 0.5934f; }
            else if (mach < 2.05f) { cd = 0.5867f; }
            else if (mach < 2.1f) { cd = 0.5804f; }
            else if (mach < 2.15f) { cd = 0.5743f; }
            else if (mach < 2.2f) { cd = 0.5685f; }
            else if (mach < 2.25f) { cd = 0.563f; }
            else if (mach < 2.3f) { cd = 0.5577f; }
            else if (mach < 2.35f) { cd = 0.5527f; }
            else if (mach < 2.4f) { cd = 0.5481f; }
            else if (mach < 2.45f) { cd = 0.5438f; }
            else if (mach < 2.5f) { cd = 0.5397f; }
            else if (mach < 2.6f) { cd = 0.5325f; }
            else if (mach < 2.7f) { cd = 0.5264f; }
            else if (mach < 2.8f) { cd = 0.5211f; }
            else if (mach < 2.9f) { cd = 0.5168f; }
            else if (mach < 3f) { cd = 0.5133f; }
            else if (mach < 3.1f) { cd = 0.5105f; }
            else if (mach < 3.2f) { cd = 0.5084f; }
            else if (mach < 3.3f) { cd = 0.5067f; }
            else if (mach < 3.4f) { cd = 0.5054f; }
            else if (mach < 3.5f) { cd = 0.504f; }
            else if (mach < 3.6f) { cd = 0.503f; }
            else if (mach < 3.7f) { cd = 0.5022f; }
            else if (mach < 3.8f) { cd = 0.5016f; }
            else if (mach < 3.9f) { cd = 0.501f; }
            else if (mach < 4f) { cd = 0.5006f; }
            else if (mach < 4.2f) { cd = 0.4998f; }
            else if (mach < 4.4f) { cd = 0.4995f; }
            else if (mach < 4.6f) { cd = 0.4992f; }
            else if (mach < 4.8f) { cd = 0.499f; }
            else if (mach < 5f) { cd = 0.4988f; }
            else if (mach > 5f) { cd = 0.4988f; }

        }

        if (drag_Model == dragModel.G2)
        {
            if (mach < 0.05f) { cd = 0.2298f; }
            else if (mach < 0.1f) { cd = 0.2287f; }
            else if (mach < 0.15f) { cd = 0.2271f; }
            else if (mach < 0.2f) { cd = 0.2251f; }
            else if (mach < 0.25f) { cd = 0.2227f; }
            else if (mach < 0.3f) { cd = 0.2196f; }
            else if (mach < 0.35f) { cd = 0.2156f; }
            else if (mach < 0.4f) { cd = 0.2107f; }
            else if (mach < 0.45f) { cd = 0.2048f; }
            else if (mach < 0.5f) { cd = 0.198f; }
            else if (mach < 0.55f) { cd = 0.1905f; }
            else if (mach < 0.6f) { cd = 0.1828f; }
            else if (mach < 0.65f) { cd = 0.1758f; }
            else if (mach < 0.7f) { cd = 0.1702f; }
            else if (mach < 0.75f) { cd = 0.1669f; }
            else if (mach < 0.78f) { cd = 0.1664f; }
            else if (mach < 0.8f) { cd = 0.1667f; }
            else if (mach < 0.83f) { cd = 0.1682f; }
            else if (mach < 0.85f) { cd = 0.1711f; }
            else if (mach < 0.88f) { cd = 0.1761f; }
            else if (mach < 0.9f) { cd = 0.1831f; }
            else if (mach < 0.93f) { cd = 0.2004f; }
            else if (mach < 0.95f) { cd = 0.2589f; }
            else if (mach < 0.98f) { cd = 0.3492f; }
            else if (mach < 1f) { cd = 0.3983f; }
            else if (mach < 1.03f) { cd = 0.4075f; }
            else if (mach < 1.05f) { cd = 0.4103f; }
            else if (mach < 1.08f) { cd = 0.4114f; }
            else if (mach < 1.1f) { cd = 0.4106f; }
            else if (mach < 1.13f) { cd = 0.4089f; }
            else if (mach < 1.15f) { cd = 0.4068f; }
            else if (mach < 1.18f) { cd = 0.4046f; }
            else if (mach < 1.2f) { cd = 0.4021f; }
            else if (mach < 1.25f) { cd = 0.3966f; }
            else if (mach < 1.3f) { cd = 0.3904f; }
            else if (mach < 1.35f) { cd = 0.3835f; }
            else if (mach < 1.4f) { cd = 0.3759f; }
            else if (mach < 1.45f) { cd = 0.3678f; }
            else if (mach < 1.5f) { cd = 0.3594f; }
            else if (mach < 1.55f) { cd = 0.3512f; }
            else if (mach < 1.6f) { cd = 0.3432f; }
            else if (mach < 1.65f) { cd = 0.3356f; }
            else if (mach < 1.7f) { cd = 0.3282f; }
            else if (mach < 1.75f) { cd = 0.3213f; }
            else if (mach < 1.8f) { cd = 0.3149f; }
            else if (mach < 1.85f) { cd = 0.3089f; }
            else if (mach < 1.9f) { cd = 0.3033f; }
            else if (mach < 1.95f) { cd = 0.2982f; }
            else if (mach < 2f) { cd = 0.2933f; }
            else if (mach < 2.05f) { cd = 0.2889f; }
            else if (mach < 2.1f) { cd = 0.2846f; }
            else if (mach < 2.15f) { cd = 0.2806f; }
            else if (mach < 2.2f) { cd = 0.2768f; }
            else if (mach < 2.25f) { cd = 0.2731f; }
            else if (mach < 2.3f) { cd = 0.2696f; }
            else if (mach < 2.35f) { cd = 0.2663f; }
            else if (mach < 2.4f) { cd = 0.2632f; }
            else if (mach < 2.45f) { cd = 0.2602f; }
            else if (mach < 2.5f) { cd = 0.2572f; }
            else if (mach < 2.55f) { cd = 0.2543f; }
            else if (mach < 2.6f) { cd = 0.2515f; }
            else if (mach < 2.65f) { cd = 0.2487f; }
            else if (mach < 2.7f) { cd = 0.246f; }
            else if (mach < 2.75f) { cd = 0.2433f; }
            else if (mach < 2.8f) { cd = 0.2408f; }
            else if (mach < 2.85f) { cd = 0.2382f; }
            else if (mach < 2.9f) { cd = 0.2357f; }
            else if (mach < 2.95f) { cd = 0.2333f; }
            else if (mach < 3f) { cd = 0.2309f; }
            else if (mach < 3.1f) { cd = 0.2262f; }
            else if (mach < 3.2f) { cd = 0.2217f; }
            else if (mach < 3.3f) { cd = 0.2173f; }
            else if (mach < 3.4f) { cd = 0.2132f; }
            else if (mach < 3.5f) { cd = 0.2091f; }
            else if (mach < 3.6f) { cd = 0.2052f; }
            else if (mach < 3.7f) { cd = 0.2014f; }
            else if (mach < 3.8f) { cd = 0.1978f; }
            else if (mach < 3.9f) { cd = 0.1944f; }
            else if (mach < 4f) { cd = 0.1912f; }
            else if (mach < 4.2f) { cd = 0.1851f; }
            else if (mach < 4.4f) { cd = 0.1794f; }
            else if (mach < 4.6f) { cd = 0.1741f; }
            else if (mach < 4.8f) { cd = 0.1693f; }
            else if (mach < 5f) { cd = 0.1648f; }
            else if (mach > 5f) { cd = 0.1648f; }

        }

        if (drag_Model == dragModel.G5)
        {
            if (mach < 0.05f) { cd = 0.1719f; }
            else if (mach < 0.1f) { cd = 0.1727f; }
            else if (mach < 0.15f) { cd = 0.1732f; }
            else if (mach < 0.2f) { cd = 0.1734f; }
            else if (mach < 0.25f) { cd = 0.173f; }
            else if (mach < 0.3f) { cd = 0.1718f; }
            else if (mach < 0.35f) { cd = 0.1696f; }
            else if (mach < 0.4f) { cd = 0.1668f; }
            else if (mach < 0.45f) { cd = 0.1637f; }
            else if (mach < 0.5f) { cd = 0.1603f; }
            else if (mach < 0.55f) { cd = 0.1566f; }
            else if (mach < 0.6f) { cd = 0.1529f; }
            else if (mach < 0.65f) { cd = 0.1497f; }
            else if (mach < 0.7f) { cd = 0.1473f; }
            else if (mach < 0.75f) { cd = 0.1463f; }
            else if (mach < 0.8f) { cd = 0.1489f; }
            else if (mach < 0.85f) { cd = 0.1583f; }
            else if (mach < 0.88f) { cd = 0.1672f; }
            else if (mach < 0.9f) { cd = 0.1815f; }
            else if (mach < 0.93f) { cd = 0.2051f; }
            else if (mach < 0.95f) { cd = 0.2413f; }
            else if (mach < 0.98f) { cd = 0.2884f; }
            else if (mach < 1f) { cd = 0.3379f; }
            else if (mach < 1.03f) { cd = 0.3785f; }
            else if (mach < 1.05f) { cd = 0.4032f; }
            else if (mach < 1.08f) { cd = 0.4147f; }
            else if (mach < 1.1f) { cd = 0.4201f; }
            else if (mach < 1.15f) { cd = 0.4278f; }
            else if (mach < 1.2f) { cd = 0.4338f; }
            else if (mach < 1.25f) { cd = 0.4373f; }
            else if (mach < 1.3f) { cd = 0.4392f; }
            else if (mach < 1.35f) { cd = 0.4403f; }
            else if (mach < 1.4f) { cd = 0.4406f; }
            else if (mach < 1.45f) { cd = 0.4401f; }
            else if (mach < 1.5f) { cd = 0.4386f; }
            else if (mach < 1.55f) { cd = 0.4362f; }
            else if (mach < 1.6f) { cd = 0.4328f; }
            else if (mach < 1.65f) { cd = 0.4286f; }
            else if (mach < 1.7f) { cd = 0.4237f; }
            else if (mach < 1.75f) { cd = 0.4182f; }
            else if (mach < 1.8f) { cd = 0.4121f; }
            else if (mach < 1.85f) { cd = 0.4057f; }
            else if (mach < 1.9f) { cd = 0.3991f; }
            else if (mach < 1.95f) { cd = 0.3926f; }
            else if (mach < 2f) { cd = 0.3861f; }
            else if (mach < 2.05f) { cd = 0.38f; }
            else if (mach < 2.1f) { cd = 0.3741f; }
            else if (mach < 2.15f) { cd = 0.3684f; }
            else if (mach < 2.2f) { cd = 0.363f; }
            else if (mach < 2.25f) { cd = 0.3578f; }
            else if (mach < 2.3f) { cd = 0.3529f; }
            else if (mach < 2.35f) { cd = 0.3481f; }
            else if (mach < 2.4f) { cd = 0.3435f; }
            else if (mach < 2.45f) { cd = 0.3391f; }
            else if (mach < 2.5f) { cd = 0.3349f; }
            else if (mach < 2.6f) { cd = 0.3269f; }
            else if (mach < 2.7f) { cd = 0.3194f; }
            else if (mach < 2.8f) { cd = 0.3125f; }
            else if (mach < 2.9f) { cd = 0.306f; }
            else if (mach < 3f) { cd = 0.2999f; }
            else if (mach < 3.1f) { cd = 0.2942f; }
            else if (mach < 3.2f) { cd = 0.2889f; }
            else if (mach < 3.3f) { cd = 0.2838f; }
            else if (mach < 3.4f) { cd = 0.279f; }
            else if (mach < 3.5f) { cd = 0.2745f; }
            else if (mach < 3.6f) { cd = 0.2703f; }
            else if (mach < 3.7f) { cd = 0.2662f; }
            else if (mach < 3.8f) { cd = 0.2624f; }
            else if (mach < 3.9f) { cd = 0.2588f; }
            else if (mach < 4f) { cd = 0.2553f; }
            else if (mach < 4.2f) { cd = 0.2488f; }
            else if (mach < 4.4f) { cd = 0.2429f; }
            else if (mach < 4.6f) { cd = 0.2376f; }
            else if (mach < 4.8f) { cd = 0.2326f; }
            else if (mach < 5f) { cd = 0.228f; }
            else if (mach > 5f) { cd = 0.228f; }

        }

        if (drag_Model == dragModel.G6)
        {
            if (mach < 0.05f) { cd = 0.2553f; }
            else if (mach < 0.1f) { cd = 0.2491f; }
            else if (mach < 0.15f) { cd = 0.2432f; }
            else if (mach < 0.2f) { cd = 0.2376f; }
            else if (mach < 0.25f) { cd = 0.2324f; }
            else if (mach < 0.3f) { cd = 0.2278f; }
            else if (mach < 0.35f) { cd = 0.2238f; }
            else if (mach < 0.4f) { cd = 0.2205f; }
            else if (mach < 0.45f) { cd = 0.2177f; }
            else if (mach < 0.5f) { cd = 0.2155f; }
            else if (mach < 0.55f) { cd = 0.2138f; }
            else if (mach < 0.6f) { cd = 0.2126f; }
            else if (mach < 0.65f) { cd = 0.2121f; }
            else if (mach < 0.7f) { cd = 0.2122f; }
            else if (mach < 0.75f) { cd = 0.2132f; }
            else if (mach < 0.8f) { cd = 0.2154f; }
            else if (mach < 0.85f) { cd = 0.2194f; }
            else if (mach < 0.88f) { cd = 0.2229f; }
            else if (mach < 0.9f) { cd = 0.2297f; }
            else if (mach < 0.93f) { cd = 0.2449f; }
            else if (mach < 0.95f) { cd = 0.2732f; }
            else if (mach < 0.98f) { cd = 0.3141f; }
            else if (mach < 1f) { cd = 0.3597f; }
            else if (mach < 1.03f) { cd = 0.3994f; }
            else if (mach < 1.05f) { cd = 0.4261f; }
            else if (mach < 1.08f) { cd = 0.4402f; }
            else if (mach < 1.1f) { cd = 0.4465f; }
            else if (mach < 1.13f) { cd = 0.449f; }
            else if (mach < 1.15f) { cd = 0.4497f; }
            else if (mach < 1.18f) { cd = 0.4494f; }
            else if (mach < 1.2f) { cd = 0.4482f; }
            else if (mach < 1.23f) { cd = 0.4464f; }
            else if (mach < 1.25f) { cd = 0.4441f; }
            else if (mach < 1.3f) { cd = 0.439f; }
            else if (mach < 1.35f) { cd = 0.4336f; }
            else if (mach < 1.4f) { cd = 0.4279f; }
            else if (mach < 1.45f) { cd = 0.4221f; }
            else if (mach < 1.5f) { cd = 0.4162f; }
            else if (mach < 1.55f) { cd = 0.4102f; }
            else if (mach < 1.6f) { cd = 0.4042f; }
            else if (mach < 1.65f) { cd = 0.3981f; }
            else if (mach < 1.7f) { cd = 0.3919f; }
            else if (mach < 1.75f) { cd = 0.3855f; }
            else if (mach < 1.8f) { cd = 0.3788f; }
            else if (mach < 1.85f) { cd = 0.3721f; }
            else if (mach < 1.9f) { cd = 0.3652f; }
            else if (mach < 1.95f) { cd = 0.3583f; }
            else if (mach < 2f) { cd = 0.3515f; }
            else if (mach < 2.05f) { cd = 0.3447f; }
            else if (mach < 2.1f) { cd = 0.3381f; }
            else if (mach < 2.15f) { cd = 0.3314f; }
            else if (mach < 2.2f) { cd = 0.3249f; }
            else if (mach < 2.25f) { cd = 0.3185f; }
            else if (mach < 2.3f) { cd = 0.3122f; }
            else if (mach < 2.35f) { cd = 0.306f; }
            else if (mach < 2.4f) { cd = 0.3f; }
            else if (mach < 2.45f) { cd = 0.2941f; }
            else if (mach < 2.5f) { cd = 0.2883f; }
            else if (mach < 2.6f) { cd = 0.2772f; }
            else if (mach < 2.7f) { cd = 0.2668f; }
            else if (mach < 2.8f) { cd = 0.2574f; }
            else if (mach < 2.9f) { cd = 0.2487f; }
            else if (mach < 3f) { cd = 0.2407f; }
            else if (mach < 3.1f) { cd = 0.2333f; }
            else if (mach < 3.2f) { cd = 0.2265f; }
            else if (mach < 3.3f) { cd = 0.2202f; }
            else if (mach < 3.4f) { cd = 0.2144f; }
            else if (mach < 3.5f) { cd = 0.2089f; }
            else if (mach < 3.6f) { cd = 0.2039f; }
            else if (mach < 3.7f) { cd = 0.1991f; }
            else if (mach < 3.8f) { cd = 0.1947f; }
            else if (mach < 3.9f) { cd = 0.1905f; }
            else if (mach < 4f) { cd = 0.1866f; }
            else if (mach < 4.2f) { cd = 0.1794f; }
            else if (mach < 4.4f) { cd = 0.173f; }
            else if (mach < 4.6f) { cd = 0.1673f; }
            else if (mach < 4.8f) { cd = 0.1621f; }
            else if (mach < 5f) { cd = 0.1574f; }
            else if (mach > 5f) { cd = 0.1574f; }

        }

        if (drag_Model == dragModel.G7)
        {

            if (mach < 0.05f) { cd = 0.1197f; }
            else if (mach < 0.1f) { cd = 0.1196f; }
            else if (mach < 0.15f) { cd = 0.1194f; }
            else if (mach < 0.2f) { cd = 0.1193f; }
            else if (mach < 0.25f) { cd = 0.1194f; }
            else if (mach < 0.3f) { cd = 0.1194f; }
            else if (mach < 0.35f) { cd = 0.1194f; }
            else if (mach < 0.4f) { cd = 0.1193f; }
            else if (mach < 0.45f) { cd = 0.1193f; }
            else if (mach < 0.5f) { cd = 0.1194f; }
            else if (mach < 0.55f) { cd = 0.1193f; }
            else if (mach < 0.6f) { cd = 0.1194f; }
            else if (mach < 0.65f) { cd = 0.1197f; }
            else if (mach < 0.7f) { cd = 0.1202f; }
            else if (mach < 0.73f) { cd = 0.1207f; }
            else if (mach < 0.75f) { cd = 0.1215f; }
            else if (mach < 0.78f) { cd = 0.1226f; }
            else if (mach < 0.8f) { cd = 0.1242f; }
            else if (mach < 0.83f) { cd = 0.1266f; }
            else if (mach < 0.85f) { cd = 0.1306f; }
            else if (mach < 0.88f) { cd = 0.1368f; }
            else if (mach < 0.9f) { cd = 0.1464f; }
            else if (mach < 0.93f) { cd = 0.166f; }
            else if (mach < 0.95f) { cd = 0.2054f; }
            else if (mach < 0.98f) { cd = 0.2993f; }
            else if (mach < 1f) { cd = 0.3803f; }
            else if (mach < 1.03f) { cd = 0.4015f; }
            else if (mach < 1.05f) { cd = 0.4043f; }
            else if (mach < 1.08f) { cd = 0.4034f; }
            else if (mach < 1.1f) { cd = 0.4014f; }
            else if (mach < 1.13f) { cd = 0.3987f; }
            else if (mach < 1.15f) { cd = 0.3955f; }
            else if (mach < 1.2f) { cd = 0.3884f; }
            else if (mach < 1.25f) { cd = 0.381f; }
            else if (mach < 1.3f) { cd = 0.3732f; }
            else if (mach < 1.35f) { cd = 0.3657f; }
            else if (mach < 1.4f) { cd = 0.358f; }
            else if (mach < 1.5f) { cd = 0.344f; }
            else if (mach < 1.55f) { cd = 0.3376f; }
            else if (mach < 1.6f) { cd = 0.3315f; }
            else if (mach < 1.65f) { cd = 0.326f; }
            else if (mach < 1.7f) { cd = 0.3209f; }
            else if (mach < 1.75f) { cd = 0.316f; }
            else if (mach < 1.8f) { cd = 0.3117f; }
            else if (mach < 1.85f) { cd = 0.3078f; }
            else if (mach < 1.9f) { cd = 0.3042f; }
            else if (mach < 1.95f) { cd = 0.301f; }
            else if (mach < 2f) { cd = 0.298f; }
            else if (mach < 2.05f) { cd = 0.2951f; }
            else if (mach < 2.1f) { cd = 0.2922f; }
            else if (mach < 2.15f) { cd = 0.2892f; }
            else if (mach < 2.2f) { cd = 0.2864f; }
            else if (mach < 2.25f) { cd = 0.2835f; }
            else if (mach < 2.3f) { cd = 0.2807f; }
            else if (mach < 2.35f) { cd = 0.2779f; }
            else if (mach < 2.4f) { cd = 0.2752f; }
            else if (mach < 2.45f) { cd = 0.2725f; }
            else if (mach < 2.5f) { cd = 0.2697f; }
            else if (mach < 2.55f) { cd = 0.267f; }
            else if (mach < 2.6f) { cd = 0.2643f; }
            else if (mach < 2.65f) { cd = 0.2615f; }
            else if (mach < 2.7f) { cd = 0.2588f; }
            else if (mach < 2.75f) { cd = 0.2561f; }
            else if (mach < 2.8f) { cd = 0.2533f; }
            else if (mach < 2.85f) { cd = 0.2506f; }
            else if (mach < 2.9f) { cd = 0.2479f; }
            else if (mach < 2.95f) { cd = 0.2451f; }
            else if (mach < 3f) { cd = 0.2424f; }
            else if (mach < 3.1f) { cd = 0.2368f; }
            else if (mach < 3.2f) { cd = 0.2313f; }
            else if (mach < 3.3f) { cd = 0.2258f; }
            else if (mach < 3.4f) { cd = 0.2205f; }
            else if (mach < 3.5f) { cd = 0.2154f; }
            else if (mach < 3.6f) { cd = 0.2106f; }
            else if (mach < 3.7f) { cd = 0.206f; }
            else if (mach < 3.8f) { cd = 0.2017f; }
            else if (mach < 3.9f) { cd = 0.1975f; }
            else if (mach < 4f) { cd = 0.1935f; }
            else if (mach < 4.2f) { cd = 0.1861f; }
            else if (mach < 4.4f) { cd = 0.1793f; }
            else if (mach < 4.6f) { cd = 0.173f; }
            else if (mach < 4.8f) { cd = 0.1672f; }
            else if (mach < 5f) { cd = 0.1618f; }
            else if (mach > 5f) { cd = 0.1618f; }

        }

        if (drag_Model == dragModel.G8)
        {
            if (mach < 0.05f) { cd = 0.2105f; }
            else if (mach < 0.1f) { cd = 0.2104f; }
            else if (mach < 0.15f) { cd = 0.2104f; }
            else if (mach < 0.2f) { cd = 0.2103f; }
            else if (mach < 0.25f) { cd = 0.2103f; }
            else if (mach < 0.3f) { cd = 0.2103f; }
            else if (mach < 0.35f) { cd = 0.2103f; }
            else if (mach < 0.4f) { cd = 0.2103f; }
            else if (mach < 0.45f) { cd = 0.2102f; }
            else if (mach < 0.5f) { cd = 0.2102f; }
            else if (mach < 0.55f) { cd = 0.2102f; }
            else if (mach < 0.6f) { cd = 0.2102f; }
            else if (mach < 0.65f) { cd = 0.2102f; }
            else if (mach < 0.7f) { cd = 0.2103f; }
            else if (mach < 0.75f) { cd = 0.2103f; }
            else if (mach < 0.8f) { cd = 0.2104f; }
            else if (mach < 0.83f) { cd = 0.2104f; }
            else if (mach < 0.85f) { cd = 0.2105f; }
            else if (mach < 0.88f) { cd = 0.2106f; }
            else if (mach < 0.9f) { cd = 0.2109f; }
            else if (mach < 0.93f) { cd = 0.2183f; }
            else if (mach < 0.95f) { cd = 0.2571f; }
            else if (mach < 0.98f) { cd = 0.3358f; }
            else if (mach < 1f) { cd = 0.4068f; }
            else if (mach < 1.03f) { cd = 0.4378f; }
            else if (mach < 1.05f) { cd = 0.4476f; }
            else if (mach < 1.08f) { cd = 0.4493f; }
            else if (mach < 1.1f) { cd = 0.4477f; }
            else if (mach < 1.13f) { cd = 0.445f; }
            else if (mach < 1.15f) { cd = 0.4419f; }
            else if (mach < 1.2f) { cd = 0.4353f; }
            else if (mach < 1.25f) { cd = 0.4283f; }
            else if (mach < 1.3f) { cd = 0.4208f; }
            else if (mach < 1.35f) { cd = 0.4133f; }
            else if (mach < 1.4f) { cd = 0.4059f; }
            else if (mach < 1.45f) { cd = 0.3986f; }
            else if (mach < 1.5f) { cd = 0.3915f; }
            else if (mach < 1.55f) { cd = 0.3845f; }
            else if (mach < 1.6f) { cd = 0.3777f; }
            else if (mach < 1.65f) { cd = 0.371f; }
            else if (mach < 1.7f) { cd = 0.3645f; }
            else if (mach < 1.75f) { cd = 0.3581f; }
            else if (mach < 1.8f) { cd = 0.3519f; }
            else if (mach < 1.85f) { cd = 0.3458f; }
            else if (mach < 1.9f) { cd = 0.34f; }
            else if (mach < 1.95f) { cd = 0.3343f; }
            else if (mach < 2f) { cd = 0.3288f; }
            else if (mach < 2.05f) { cd = 0.3234f; }
            else if (mach < 2.1f) { cd = 0.3182f; }
            else if (mach < 2.15f) { cd = 0.3131f; }
            else if (mach < 2.2f) { cd = 0.3081f; }
            else if (mach < 2.25f) { cd = 0.3032f; }
            else if (mach < 2.3f) { cd = 0.2983f; }
            else if (mach < 2.35f) { cd = 0.2937f; }
            else if (mach < 2.4f) { cd = 0.2891f; }
            else if (mach < 2.45f) { cd = 0.2845f; }
            else if (mach < 2.5f) { cd = 0.2802f; }
            else if (mach < 2.6f) { cd = 0.272f; }
            else if (mach < 2.7f) { cd = 0.2642f; }
            else if (mach < 2.8f) { cd = 0.2569f; }
            else if (mach < 2.9f) { cd = 0.2499f; }
            else if (mach < 3f) { cd = 0.2432f; }
            else if (mach < 3.1f) { cd = 0.2368f; }
            else if (mach < 3.2f) { cd = 0.2308f; }
            else if (mach < 3.3f) { cd = 0.2251f; }
            else if (mach < 3.4f) { cd = 0.2197f; }
            else if (mach < 3.5f) { cd = 0.2147f; }
            else if (mach < 3.6f) { cd = 0.2101f; }
            else if (mach < 3.7f) { cd = 0.2058f; }
            else if (mach < 3.8f) { cd = 0.2019f; }
            else if (mach < 3.9f) { cd = 0.1983f; }
            else if (mach < 4f) { cd = 0.195f; }
            else if (mach < 4.2f) { cd = 0.189f; }
            else if (mach < 4.4f) { cd = 0.1837f; }
            else if (mach < 4.6f) { cd = 0.1791f; }
            else if (mach < 4.8f) { cd = 0.175f; }
            else if (mach < 5f) { cd = 0.1713f; }
            else if (mach > 5f) { cd = 0.1713f; }

        }

        if (drag_Model == dragModel.GS)
        {
            if (mach < 0.05f) { cd = 0.4689f; }
            else if (mach < 0.1f) { cd = 0.4717f; }
            else if (mach < 0.15f) { cd = 0.4745f; }
            else if (mach < 0.2f) { cd = 0.4772f; }
            else if (mach < 0.25f) { cd = 0.48f; }
            else if (mach < 0.3f) { cd = 0.4827f; }
            else if (mach < 0.35f) { cd = 0.4852f; }
            else if (mach < 0.4f) { cd = 0.4882f; }
            else if (mach < 0.45f) { cd = 0.492f; }
            else if (mach < 0.5f) { cd = 0.497f; }
            else if (mach < 0.55f) { cd = 0.508f; }
            else if (mach < 0.6f) { cd = 0.526f; }
            else if (mach < 0.65f) { cd = 0.559f; }
            else if (mach < 0.7f) { cd = 0.592f; }
            else if (mach < 0.75f) { cd = 0.6258f; }
            else if (mach < 0.8f) { cd = 0.661f; }
            else if (mach < 0.85f) { cd = 0.6985f; }
            else if (mach < 0.9f) { cd = 0.737f; }
            else if (mach < 0.95f) { cd = 0.7757f; }
            else if (mach < 1f) { cd = 0.814f; }
            else if (mach < 1.05f) { cd = 0.8512f; }
            else if (mach < 1.1f) { cd = 0.887f; }
            else if (mach < 1.15f) { cd = 0.921f; }
            else if (mach < 1.2f) { cd = 0.951f; }
            else if (mach < 1.25f) { cd = 0.974f; }
            else if (mach < 1.3f) { cd = 0.991f; }
            else if (mach < 1.35f) { cd = 0.999f; }
            else if (mach < 1.4f) { cd = 1.003f; }
            else if (mach < 1.45f) { cd = 1.006f; }
            else if (mach < 1.5f) { cd = 1.008f; }
            else if (mach < 1.55f) { cd = 1.009f; }
            else if (mach < 1.6f) { cd = 1.009f; }
            else if (mach < 1.65f) { cd = 1.009f; }
            else if (mach < 1.7f) { cd = 1.009f; }
            else if (mach < 1.75f) { cd = 1.008f; }
            else if (mach < 1.8f) { cd = 1.007f; }
            else if (mach < 1.85f) { cd = 1.006f; }
            else if (mach < 1.9f) { cd = 1.004f; }
            else if (mach < 1.95f) { cd = 1.0025f; }
            else if (mach < 2f) { cd = 1.001f; }
            else if (mach < 2.05f) { cd = 0.999f; }
            else if (mach < 2.1f) { cd = 0.997f; }
            else if (mach < 2.15f) { cd = 0.9956f; }
            else if (mach < 2.2f) { cd = 0.994f; }
            else if (mach < 2.25f) { cd = 0.9916f; }
            else if (mach < 2.3f) { cd = 0.989f; }
            else if (mach < 2.35f) { cd = 0.9869f; }
            else if (mach < 2.4f) { cd = 0.985f; }
            else if (mach < 2.45f) { cd = 0.983f; }
            else if (mach < 2.5f) { cd = 0.981f; }
            else if (mach < 2.55f) { cd = 0.979f; }
            else if (mach < 2.6f) { cd = 0.977f; }
            else if (mach < 2.65f) { cd = 0.975f; }
            else if (mach < 2.7f) { cd = 0.973f; }
            else if (mach < 2.75f) { cd = 0.971f; }
            else if (mach < 2.8f) { cd = 0.969f; }
            else if (mach < 2.85f) { cd = 0.967f; }
            else if (mach < 2.9f) { cd = 0.965f; }
            else if (mach < 2.95f) { cd = 0.963f; }
            else if (mach < 3f) { cd = 0.961f; }
            else if (mach < 3.05f) { cd = 0.9589f; }
            else if (mach < 3.1f) { cd = 0.957f; }
            else if (mach < 3.15f) { cd = 0.9555f; }
            else if (mach < 3.2f) { cd = 0.954f; }
            else if (mach < 3.25f) { cd = 0.952f; }
            else if (mach < 3.3f) { cd = 0.95f; }
            else if (mach < 3.35f) { cd = 0.9485f; }
            else if (mach < 3.4f) { cd = 0.947f; }
            else if (mach < 3.45f) { cd = 0.945f; }
            else if (mach < 3.5f) { cd = 0.943f; }
            else if (mach < 3.55f) { cd = 0.9414f; }
            else if (mach < 3.6f) { cd = 0.94f; }
            else if (mach < 3.65f) { cd = 0.9385f; }
            else if (mach < 3.7f) { cd = 0.937f; }
            else if (mach < 3.75f) { cd = 0.9355f; }
            else if (mach < 3.8f) { cd = 0.934f; }
            else if (mach < 3.85f) { cd = 0.9325f; }
            else if (mach < 3.9f) { cd = 0.931f; }
            else if (mach < 3.95f) { cd = 0.9295f; }
            else if (mach < 4f) { cd = 0.928f; }
            else if (mach > 4f) { cd = 0.928f; }

        }

        return cd;
    }


    public float GetStabilityFactor()
    {

        //Improved Miller Formula
        unitConversion();

        float stabilityFactor;

        stabilityFactor = 30f * this.grains / (Mathf.Pow(this.twist_Calibers, 2) * Mathf.Pow(this.bullet_Dia_Inch, 3) * this.calibers * (1 + Mathf.Pow(this.calibers, 2)));
        stabilityFactor = stabilityFactor * Mathf.Pow((this.muzzle_Vel_Fps / 2800f), (1/3));
        stabilityFactor = stabilityFactor * ((this.temp_F + 460f) / (519f)) * (29.92f / this.baro);

        return stabilityFactor;
    }

    private void unitConversion()
    {

        this.muzzle_Vel_Fps = this.muzzle_Velocity * 3.28084f;
        this.baro = this.pressure * 0.0002953f;
        this.temp_F = ((this.temp_k - 273.15f) * 1.8f) + 32;

    }

    private float[] vectorNormalize(float[] input)
    {

        float[] outpout = { 0, 0, 0 };

        outpout[0] = input[0] / vectorlength(input);
        outpout[1] = input[1] / vectorlength(input);
        outpout[2] = input[2] / vectorlength(input);

        return outpout;
    }

    private float vectorlength(float[] input)
    {
        return Mathf.Sqrt(Mathf.Pow(input[0], 2) + Mathf.Pow(input[1], 2) + Mathf.Pow(input[2], 2)); ;
    }

    private float[] vectorOperation(float[] input1, float[] input2, string _operator)
    {

        float[] output = { 0, 0, 0 };

        // Vector Addation
        if (_operator == "+")
        {
            output[0] = input1[0] + input2[0];
            output[1] = input1[1] + input2[1];
            output[2] = input1[2] + input2[2];
        }
        // Vector Subtraction
        else if (_operator == "-")
        {
            output[0] = input1[0] - input2[0];
            output[1] = input1[1] - input2[1];
            output[2] = input1[2] - input2[2];
        }
        // Vector Dot Product
        else if (_operator == "*")
        {
            output[0] = input1[0] * input2[0];
            output[1] = input1[1] * input2[1];
            output[2] = input1[2] * input2[2];
        }
        return output;
    }

}


