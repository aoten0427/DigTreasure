using UnityEngine;
using VoxelWorld;

/// <summary>
/// ボクセルエフェクトの管理
/// </summary>
public class VoxelEffectManager : MonoBehaviour
{
    [SerializeField] GameObject m_effect;

    public void SpownEffect(Vector3 pos,int voxelNum,Vector3 direction,int voxelid)
    {
        //エフェクトを生成
        GameObject effectObj = Instantiate(m_effect,pos,Quaternion.identity,transform);
        VoxelEffect effect = effectObj.GetComponent<VoxelEffect>();
        if(effect == null)
        {
            Destroy(effectObj);
            return;
        }

        effect.Rewind(voxelNum,direction,voxelid);
    }
}
