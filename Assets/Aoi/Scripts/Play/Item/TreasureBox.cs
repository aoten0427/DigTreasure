using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class TreasureBox : NetworkBehaviour
{
    [Networked] NetworkBool m_isOpen { get; set; }
    [SerializeField] Mesh m_openMehs;
    [SerializeField] Mesh m_closeMehs;
    [SerializeField] MeshFilter m_filter;

    [SerializeField] Treasure m_treasurePrefab;

    private TreasureList m_treasureList;

    List<Rigidbody> m_createTreasure = new List<Rigidbody>();

    [SerializeField] Vector2 m_offsetX;
    [SerializeField] Vector2 m_offsetY;

    [SerializeField] GameObject m_target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_filter.mesh = m_closeMehs;
        m_treasureList = Resources.Load<TreasureList>("Treasure/TreasureList");
    }


    public override void Spawned()
    {
        
    }



    public override void FixedUpdateNetwork()
    {
        MoveTreasure();
    }

    void Open(GameObject target)
    {
        // NetworkObject‚©‚çNetworkId‚ğæ“¾‚µ‚Ä“n‚·
        var networkObject = target.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            RPC_IsOpen(networkObject.Id);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_IsOpen(NetworkId targetId)
    {
        if (m_isOpen) return;

        // NetworkId‚©‚çNetworkObject‚ğæ“¾‚µAGameObject‚ğ“¾‚é
        if (Runner.TryFindObject(targetId, out NetworkObject networkObject))
        {
            m_target = networkObject.gameObject;
        }

        m_isOpen = true;
        m_filter.mesh = m_openMehs;

        for (int i = 0; i < 15; i++)
        {
            TreasureSpown();
        }
        TreasureSpown();

        RPC_Open();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Open()
    {
        m_filter.mesh = m_openMehs;
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<IPlayerAttack>();
        if(player != null)
        {
            Open(player.Player.gameObject);
        }
    }

    void Close()
    {
        m_filter.mesh = m_closeMehs;
    }

    void TreasureSpown()
    {
        int selectindex = Random.Range(0, m_treasureList.allTreasure.Count);
        int point = m_treasureList.allTreasure[selectindex].point;


        float offsetx = Random.Range(m_offsetX.x, m_offsetX.y);
        Vector3 offset = new Vector3(Random.Range(m_offsetX.x, m_offsetX.y), Random.Range(m_offsetY.x, m_offsetY.y), 0);
        offset = transform.rotation * offset;
        offset += transform.position;

        Runner.Spawn(m_treasurePrefab, offset, Quaternion.identity, onBeforeSpawned: (runner, obj) =>
        {
            Treasure treasure = obj.GetComponent<Treasure>();
            treasure.SetScorePoint(point);
            treasure.SetMeshIndex(selectindex);
            treasure.IsInvalid = false;
            treasure.InvalidTime = 0.0f;
           

            Rigidbody rigidbody = obj.GetComponent<Rigidbody>();
            rigidbody.useGravity = false;
            m_createTreasure.Add(rigidbody);

            //obj.transform.localScale *= 0.5f;
        });


    }

    void MoveTreasure()
    {
        foreach(var rb in m_createTreasure) {
            if (rb == null) return;
            Vector3 dire = (m_target.transform.position + new Vector3(0,1,0)) - rb.transform.position;
            //if (dire.sqrMagnitude < 0.1f) continue;
            
            rb.AddForce(dire * 5 * Time.deltaTime,ForceMode.Impulse);
        }
    }
}
