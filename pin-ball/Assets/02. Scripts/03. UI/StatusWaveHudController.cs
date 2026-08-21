using UnityEngine;
using UnityEngine.UI;

internal sealed class StatusWaveHudController
{
    private const int WaveNodeCount = 10;
    private const int WaveConnectorCount = WaveNodeCount - 1;

    private readonly Image[] nodes;
    private readonly Image[] connectors;
    private readonly Sprite idleNode;
    private readonly Sprite lockedNode;
    private readonly Sprite currentNode;
    private readonly Sprite completeNode;
    private readonly Sprite elite05Node;
    private readonly Sprite elite09Node;
    private readonly Sprite boss10Node;
    private readonly Sprite idleConnector;
    private readonly Sprite completeConnector;
    private readonly WaveHudState state = new();

    public StatusWaveHudController(
        Image[] nodes,
        Image[] connectors,
        Sprite idleNode,
        Sprite lockedNode,
        Sprite currentNode,
        Sprite completeNode,
        Sprite elite05Node,
        Sprite elite09Node,
        Sprite boss10Node,
        Sprite idleConnector,
        Sprite completeConnector)
    {
        this.nodes = nodes;
        this.connectors = connectors;
        this.idleNode = idleNode;
        this.lockedNode = lockedNode;
        this.currentNode = currentNode;
        this.completeNode = completeNode;
        this.elite05Node = elite05Node;
        this.elite09Node = elite09Node;
        this.boss10Node = boss10Node;
        this.idleConnector = idleConnector;
        this.completeConnector = completeConnector;
    }

    public bool ValidateReferences()
    {
        bool valid = nodes != null && nodes.Length == WaveNodeCount &&
                     connectors != null &&
                     connectors.Length == WaveConnectorCount;
        if (valid)
        {
            foreach (Image node in nodes) valid &= node != null;
            foreach (Image connector in connectors) valid &= connector != null;
        }

        valid &= idleNode != null && lockedNode != null &&
                 currentNode != null && completeNode != null &&
                 elite05Node != null && elite09Node != null &&
                 boss10Node != null && idleConnector != null &&
                 completeConnector != null;
        if (!valid)
        {
            Debug.LogError(
                "[StatusPanel] Wave HUD requires 10 nodes, 9 connectors, " +
                "and all state sprites.");
        }
        return valid;
    }

    public bool SupportsWaveCount(int waveCount) =>
        state.IsSupportedWaveCount(waveCount);

    public void Display(int currentWave)
    {
        for (var index = 0; index < WaveNodeCount; index++)
        {
            nodes[index].gameObject.SetActive(true);
            nodes[index].sprite = GetNodeSprite(
                state.ResolveNodeState(currentWave, index + 1));
        }

        for (var index = 0; index < WaveConnectorCount; index++)
        {
            connectors[index].gameObject.SetActive(true);
            connectors[index].sprite =
                state.IsConnectorComplete(currentWave, index + 1)
                    ? completeConnector
                    : idleConnector;
        }
    }

    private Sprite GetNodeSprite(EWaveHudNodeState nodeState) =>
        nodeState switch
        {
            EWaveHudNodeState.Current => currentNode,
            EWaveHudNodeState.Complete => completeNode,
            EWaveHudNodeState.Elite05 => elite05Node,
            EWaveHudNodeState.Elite09 => elite09Node,
            EWaveHudNodeState.Boss10 => boss10Node,
            EWaveHudNodeState.Locked => lockedNode,
            _ => idleNode
        };
}
