using UnityEngine;
using UnityEngine.UI;

internal sealed class StatusWaveHudController
{
    private const int WaveNodeCount = 10;
    private const int WaveConnectorCount = WaveNodeCount - 1;

    private readonly Image[] _nodes;
    private readonly Image[] _connectors;
    private readonly Sprite _idleNode;
    private readonly Sprite _lockedNode;
    private readonly Sprite _currentNode;
    private readonly Sprite _completeNode;
    private readonly Sprite _elite05Node;
    private readonly Sprite _elite09Node;
    private readonly Sprite _boss10Node;
    private readonly Sprite _idleConnector;
    private readonly Sprite _completeConnector;
    private readonly WaveHudState _state = new();

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
        _nodes = nodes;
        _connectors = connectors;
        _idleNode = idleNode;
        _lockedNode = lockedNode;
        _currentNode = currentNode;
        _completeNode = completeNode;
        _elite05Node = elite05Node;
        _elite09Node = elite09Node;
        _boss10Node = boss10Node;
        _idleConnector = idleConnector;
        _completeConnector = completeConnector;
    }

    public bool ValidateReferences()
    {
        bool valid =
            _nodes != null &&
            _nodes.Length == WaveNodeCount &&
            _connectors != null &&
            _connectors.Length == WaveConnectorCount;

        if (valid)
        {
            foreach (var node in _nodes) valid &= node != null;
            foreach (var connector in _connectors)
            {
                valid &= connector != null;
            }
        }

        valid &= _idleNode != null;
        valid &= _lockedNode != null;
        valid &= _currentNode != null;
        valid &= _completeNode != null;
        valid &= _elite05Node != null;
        valid &= _elite09Node != null;
        valid &= _boss10Node != null;
        valid &= _idleConnector != null;
        valid &= _completeConnector != null;

        if (!valid)
        {
            Debug.LogError(
                "[StatusPanel] Wave HUD requires 10 nodes, " +
                "9 connectors, standard-wave labels, and all state Sprites.");
        }

        return valid;
    }

    public bool SupportsWaveCount(int waveCount) =>
        _state.IsSupportedWaveCount(waveCount);

    public void Display(int currentWave)
    {
        for (int index = 0; index < WaveNodeCount; index++)
        {
            int nodeWave = index + 1;
            _nodes[index].sprite = GetNodeSprite(
                _state.ResolveNodeState(currentWave, nodeWave));
        }

        for (int index = 0; index < WaveConnectorCount; index++)
        {
            int connectorAfterWave = index + 1;
            _connectors[index].sprite =
                _state.IsConnectorComplete(currentWave, connectorAfterWave)
                    ? _completeConnector
                    : _idleConnector;
        }
    }

    private Sprite GetNodeSprite(EWaveHudNodeState state)
    {
        return state switch
        {
            EWaveHudNodeState.Current => _currentNode,
            EWaveHudNodeState.Complete => _completeNode,
            EWaveHudNodeState.Elite05 => _elite05Node,
            EWaveHudNodeState.Elite09 => _elite09Node,
            EWaveHudNodeState.Boss10 => _boss10Node,
            EWaveHudNodeState.Locked => _lockedNode,
            _ => _idleNode
        };
    }
}
