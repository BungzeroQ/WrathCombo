using Dalamud.Game.ClientState.Objects.Types;

public class WrathPartyMember
{
    public bool HPUpdatePending = false;
    public bool MPUpdatePending = false;
    public ulong GameObjectId;
    public IBattleChara BattleChara = null!;

    // เพิ่มตัวแปรสำหรับเก็บค่า HP และ MP
    private uint _currentHP;
    private uint _currentMP;

    public uint CurrentHP
    {
        get
        {
            if ((_currentHP > BattleChara.CurrentHp && !HPUpdatePending) || _currentHP < BattleChara.CurrentHp)
                _currentHP = BattleChara.CurrentHp;

            return _currentHP;
        }
        set => _currentHP = value;
    }

    public uint CurrentMP
    {
        get
        {
            if ((_currentMP > BattleChara.CurrentMp && !MPUpdatePending) || _currentMP < BattleChara.CurrentMp)
                _currentMP = BattleChara.CurrentMp;

            return _currentMP;
        }
        set => _currentMP = value;
    }
}