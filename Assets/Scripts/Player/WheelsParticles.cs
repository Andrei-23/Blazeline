using UnityEngine;

public class WheelsParticles : MonoBehaviour
{
    // [Header("Back wheels")]
    [SerializeField] private ParticleSystem[] backWheelPSArray;
    [SerializeField] private float backWheelBaseROD = 10f;
    
    // [Header("Drift wheels - left")]
    [SerializeField] private ParticleSystem[] driftWheelPSArrayL;
    [SerializeField] private float driftWheelLBaseROD = 10f;

    // [Header("Drift wheels - right")]
    [SerializeField] private ParticleSystem[] driftWheelPSArrayR;
    [SerializeField] private float driftWheelRBaseROD = 10f;

    
    public float ForceBack {get; private set;}
    public float ForceDriftL {get; private set;}
    public float ForceDriftR {get; private set;}

    void Update()
    {
        UpdateParticleRate();
    }

    private void UpdateParticleRate(){
        foreach(ParticleSystem p in backWheelPSArray){
            var emission = p.emission;
            emission.rateOverDistance = backWheelBaseROD * ForceBack;
        }
        foreach(ParticleSystem p in driftWheelPSArrayL){
            var emission = p.emission;
            emission.rateOverDistance = driftWheelLBaseROD * ForceDriftL;
        }
        foreach(ParticleSystem p in driftWheelPSArrayR){
            var emission = p.emission;
            emission.rateOverDistance = driftWheelRBaseROD * ForceDriftR;
        }
    }

    public void SetParticlePower(float forceBack, float forceDriftL, float forceDriftR){
        ForceBack = forceBack;
        ForceDriftL = forceDriftL;
        ForceDriftR = forceDriftR;
    }

    /// <summary>
    /// Uses drift rotation to set particle forces
    /// </summary>
    /// <param name="driftAgnle">How much the board is rotated by drift, [-1..1]</param>
    public void SetParticlePower(float driftAgnle){
        if(driftAgnle > 0){
            ForceDriftL = 0;
            ForceDriftR = driftAgnle * driftAgnle;
        }
        else{
            ForceDriftL = driftAgnle * driftAgnle;
            ForceDriftR = 0;
        }
        ForceBack = 1 - driftAgnle * driftAgnle;
    }
}
