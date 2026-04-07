using UnityEngine;
using System.Runtime.InteropServices;

public struct FluidParticle
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 force;
    public float density;
    public float pressure;
}

public class FluidManager : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material particleMaterial;

    [Header("Simulation Settings")]
    public int numParticles = 4000;
    public float gravity = 9.8f;
    public float particleMass = 1.0f;
    public float smoothingRadius = 0.5f;
    public float targetDensity = 1.0f;
    public float pressureMultiplier = 200.0f;
    public float viscosity = 0.5f;

    [Header("Environment")]
    public Vector3 boundsSize = new Vector3(8, 8, 8);

    private ComputeBuffer particleBuffer;
    private int densityPressureKernel;
    private int forcesKernel;
    private int integrateKernel;
    private int threadGroups;

    void Start()
    {
        InitializeBuffers();
        InitializeComputeShader();
    }

    void InitializeBuffers()
    {
        FluidParticle[] particles = new FluidParticle[numParticles];
        
        // Spawn particles in a smaller central box so they can fall and splash
        for (int i = 0; i < numParticles; i++)
        {
            particles[i].position = new Vector3(
                Random.Range(-boundsSize.x / 4f, boundsSize.x / 4f),
                Random.Range(0f, boundsSize.y / 2f),
                Random.Range(-boundsSize.z / 4f, boundsSize.z / 4f)
            );
            particles[i].velocity = Vector3.zero;
            particles[i].force = Vector3.zero;
            particles[i].density = 0f;
            particles[i].pressure = 0f;
        }

        particleBuffer = new ComputeBuffer(numParticles, Marshal.SizeOf(typeof(FluidParticle)));
        particleBuffer.SetData(particles);
    }

    void InitializeComputeShader()
    {
        densityPressureKernel = computeShader.FindKernel("ComputeDensityPressure");
        forcesKernel = computeShader.FindKernel("ComputeForces");
        integrateKernel = computeShader.FindKernel("Integrate");

        // We use 256 threads per group in the compute shader
        threadGroups = Mathf.CeilToInt(numParticles / 256.0f);

        computeShader.SetBuffer(densityPressureKernel, "_Particles", particleBuffer);
        computeShader.SetBuffer(forcesKernel, "_Particles", particleBuffer);
        computeShader.SetBuffer(integrateKernel, "_Particles", particleBuffer);
    }

    void Update()
    {
        UpdateConstants();

        // 3 passes of SPH algorithm
        computeShader.Dispatch(densityPressureKernel, threadGroups, 1, 1);
        computeShader.Dispatch(forcesKernel, threadGroups, 1, 1);
        computeShader.Dispatch(integrateKernel, threadGroups, 1, 1);
    }

    void UpdateConstants()
    {
        computeShader.SetInt("_NumParticles", numParticles);
        computeShader.SetFloat("_Gravity", gravity);
        computeShader.SetFloat("_ParticleMass", particleMass);
        computeShader.SetFloat("_SmoothingRadius", smoothingRadius);
        computeShader.SetFloat("_TargetDensity", targetDensity);
        computeShader.SetFloat("_PressureMultiplier", pressureMultiplier);
        computeShader.SetFloat("_Viscosity", viscosity);
        
        // Clamp delta time to avoid physics explosion on lag spikes
        computeShader.SetFloat("_DeltaTime", Mathf.Min(Time.deltaTime, 0.02f)); 
        computeShader.SetVector("_BoundsSize", boundsSize);

        // Calculate and pass constant coefficients for SPH kernels to save GPU cycles
        float pi = Mathf.PI;
        computeShader.SetFloat("_Poly6Constant", 315.0f / (64.0f * pi * Mathf.Pow(smoothingRadius, 9.0f)));
        computeShader.SetFloat("_SpikyConstant", 45.0f / (pi * Mathf.Pow(smoothingRadius, 6.0f)));
        computeShader.SetFloat("_ViscConstant", 45.0f / (pi * Mathf.Pow(smoothingRadius, 6.0f)));
    }

    void OnRenderObject()
    {
        // Procedurally draw particles every frame using the GPU buffer directly (No CPU iteration needed)
        if (particleMaterial != null && particleBuffer != null)
        {
            particleMaterial.SetPass(0);
            particleMaterial.SetBuffer("_Particles", particleBuffer);
            Graphics.DrawProceduralNow(MeshTopology.Points, numParticles);
        }
    }

    void OnDestroy()
    {
        if (particleBuffer != null)
        {
            particleBuffer.Release();
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, boundsSize);
    }
}
