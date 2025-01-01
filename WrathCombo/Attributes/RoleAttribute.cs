using ECommons.GameHelpers;
using System;
using ECommons.ExcelServices;

namespace WrathCombo.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    internal class RoleAttribute : Attribute
    {
        public JobRole Role;
        internal RoleAttribute(JobRole role)
        {
            Role = role;
        }

        internal bool PlayerIsRole()
        {
            if (Role == JobRole.All)
                return true;

            switch (Player.Job)
            {
                case Job.GLA:
                case Job.PLD:
                case Job.MRD:
                case Job.WAR:
                case Job.DRK:
                case Job.GNB:
                    return Role == JobRole.Tank;
                case Job.CNJ:
                case Job.WHM:
                case Job.AST:
                case Job.SCH:
                case Job.SGE:
                    return Role == JobRole.Healer;
                case Job.ARC:
                case Job.BRD:
                case Job.MCH:
                case Job.DNC:
                    return Role == JobRole.RangedDPS;
                case Job.THM:
                case Job.BLM:
                case Job.ACN:
                case Job.SMN:
                case Job.RDM:
                case Job.PCT:
                case Job.BLU:
                    return Role == JobRole.MagicalDPS;
                case Job.LNC:
                case Job.DRG:
                case Job.PGL:
                case Job.MNK:
                case Job.ROG:
                case Job.NIN:
                case Job.SAM:
                case Job.VPR:
                case Job.RPR:
                    return Role == JobRole.MeleeDPS;
                case Job.BTN:
                case Job.MIN:
                case Job.FSH:
                    return Role == JobRole.DoL;
                case Job.CRP:
                case Job.GSM:
                case Job.LTW:
                case Job.CUL:
                case Job.BSM:
                case Job.ARM:
                case Job.ALC:
                case Job.WVR:
                    return Role == JobRole.DoH;
            }

            return false;
        }
    }

    public enum JobRole
    {
        All,
        Tank,
        Healer,
        MeleeDPS,
        RangedDPS,
        MagicalDPS,
        DoH,
        DoL
    }
}
