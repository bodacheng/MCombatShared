using UnityEngine;

public class KnockOffCount
{
    float knockOffTimeCounter;
    readonly float knockOffCooldownInfer;
    float knockOffGauge;

    public KnockOffCount()
    {
        knockOffTimeCounter = 1f;
        knockOffCooldownInfer = 1f;
        knockOffGauge = 0;
    }

    public void SetGauge(float amount)
    {
        knockOffGauge = amount;
    }

    public void PlusGauge(float amount)
    {
        knockOffGauge += amount;
    }

    public void PlusTimeCounter(float timeAmount)
    {
        knockOffTimeCounter += timeAmount;
    }

    public float GetGauge()
    {
        return knockOffGauge;
    }

    public void Update()
    {
        if (knockOffGauge <= 0)
        {
            return;
        }

        if (knockOffTimeCounter > 0)
        {
            knockOffTimeCounter -= Time.fixedDeltaTime;
        }

        if (knockOffTimeCounter <= 0)
        {
            knockOffGauge = Mathf.Clamp(knockOffGauge - 2f, 0, Mathf.Infinity);
            knockOffTimeCounter = knockOffCooldownInfer;
        }
    }
}

public class BeHitCount
{
    int beHitCount;
    float beHitComboTimeCounter;
    readonly float hitConnectTolerate;

    public BeHitCount()
    {
        beHitCount = 0;
        hitConnectTolerate = 1.5f;
        beHitComboTimeCounter = 0f;
    }

    public void BeHitCountPlus()
    {
        beHitComboTimeCounter = hitConnectTolerate;
        beHitCount += 1;
    }

    public void BeHitCountInterrupt()
    {
        beHitCount = 0;
        beHitComboTimeCounter = 0;
    }

    public int GetBeHitCount()
    {
        return beHitCount;
    }

    public void Update()
    {
        if (beHitComboTimeCounter <= 0f)
        {
            return;
        }

        beHitComboTimeCounter -= Time.fixedDeltaTime;
        if (beHitComboTimeCounter <= 0f)
        {
            beHitCount = 0;
        }
    }
}
