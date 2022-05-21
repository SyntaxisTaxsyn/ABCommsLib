using libplctag;
using libplctag.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Net.NetworkInformation;

namespace ABComms;

    public class PLC : IDisposable
    {
        public ParentPLCData Data;
        public string IpAddress;
        public List<TAG> Tags;

        public PLC(string name,
                      string ipaddress,
                      int slot,
                      int timeout)
        {
            Data = new ParentPLCData();
            Data.Name = name;
            Data.IPAddress = ipaddress;
            Data.Route = "1," + slot.ToString();
            Data.Timeout = TimeSpan.FromSeconds(timeout);
            Tags = new List<TAG>();
            IpAddress = ipaddress; // this is here only to easily read the property back out for ID purposes
        }

        public void AddTag(string tag, TAGType type)
        {
            TAG _newtag = new TAG(Data, type, tag);
            Tags.Add(_newtag);
        }

        public void AddTag(string tag, TAGType type, bool active)
        {
            TAG _newtag = new TAG(Data, type, tag);
            _newtag.SetActive(active);
            Tags.Add(_newtag);
        }

        public void AddTag(TAG newtag)
        {
            Tags.Add(newtag);
        }

        public void Dispose()
        {
            foreach (var tag in Tags)
            {
                tag.Dispose();
            }
        }

        public bool PingPLCDevice()
        {

            bool success = false;
            int retrycount = 0;

            for (int i = 0; i < 5; i++)
            {
                if (!success && retrycount < 6)
                {
                    if (PingHost(this.Data.IPAddress))
                    {
                        success = true;
                        break;
                    }
                    else
                    {
                        retrycount++;
                    }
                }
            }

            return success;
        }

        private bool PingHost(string IpAddress)
        {
            bool pingable = false;
            Ping? pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(IpAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

    }

    public class TAG : IDisposable
    {
        string _tag;
        TAGType _type;
        public string TagName;

        Tag<CustomBoolPlcMapper, bool> _boolTag;
        Tag<IntPlcMapper, short> _intTag;
        Tag<DintPlcMapper, int> _dintTag;
        Tag<RealPlcMapper, float> _realTag;
        Tag<StringPlcMapper, string> _stringTag;

        private bool _boolPrevious;
        private short _intPrevious;
        private int _dintPrevious;
        private float _realPrevious;
        private string? _stringPrevious;

        public EventHandler DataChanged;
        private System.Timers.Timer? UpdateCycle;
        private int UpdateTime_ms = 1000;
        private bool _active;

        public void Dispose()
        {
            if (UpdateCycle != null && _active)
            {
                UpdateCycle.Elapsed -= UpdateTimer_Tick;
                UpdateCycle.Stop();
                UpdateCycle.Dispose();
            }

            switch (_type)
            {
                case TAGType.Bool:
                    _boolTag.Dispose();
                    break;
                case TAGType.Int:
                    _intTag.Dispose();
                    break;
                case TAGType.Dint:
                    _dintTag.Dispose();
                    break;
                case TAGType.Real:
                    _realTag.Dispose();
                    break;
                case TAGType.String:
                    _stringTag.Dispose();
                    break;
                default:
                    break;
            }
        }

        public void SetActive(bool val)
        {
            _active = val;
            if (_active)
            {
                ActivateUpdateTimer(true);
            }
            else
            {
                ActivateUpdateTimer(false);
            }
        }

        public void SetActive(bool val, double updaterate_seconds)
        {
            _active = val;
            UpdateTime_ms = Convert.ToInt32(Math.Round(updaterate_seconds * 1000, MidpointRounding.ToZero));
            if (_active)
            {
                ActivateUpdateTimer(true);
            }
            else
            {
                ActivateUpdateTimer(false);
            }
        }

        public bool GetActive()
        {
            return _active;
        }

        public TAG(ParentPLCData plc, TAGType type, string tag)
        {
            switch (type)
            {
                case TAGType.Bool:
                    _boolTag = new Tag<CustomBoolPlcMapper, bool>()
                    {
                        Name = tag,
                        Gateway = plc.IPAddress,
                        Path = plc.Route,
                        PlcType = plc.PLC_Type,
                        Protocol = plc.Protocol,
                        Timeout = plc.Timeout
                    };
                    break;
                case TAGType.Int:
                    _intTag = new Tag<IntPlcMapper, short>()
                    {
                        Name = tag,
                        Gateway = plc.IPAddress,
                        Path = plc.Route,
                        PlcType = plc.PLC_Type,
                        Protocol = plc.Protocol,
                        Timeout = plc.Timeout
                    };
                    break;
                case TAGType.Dint:
                    _dintTag = new Tag<DintPlcMapper, int>()
                    {
                        Name = tag,
                        Gateway = plc.IPAddress,
                        Path = plc.Route,
                        PlcType = plc.PLC_Type,
                        Protocol = plc.Protocol,
                        Timeout = plc.Timeout
                    };
                    break;
                case TAGType.Real:
                    _realTag = new Tag<RealPlcMapper, float>()
                    {
                        Name = tag,
                        Gateway = plc.IPAddress,
                        Path = plc.Route,
                        PlcType = plc.PLC_Type,
                        Protocol = plc.Protocol,
                        Timeout = plc.Timeout
                    };
                    break;
                case TAGType.String:
                    _stringTag = new Tag<StringPlcMapper, string>()
                    {
                        Name = tag,
                        Gateway = plc.IPAddress,
                        Path = plc.Route,
                        PlcType = plc.PLC_Type,
                        Protocol = plc.Protocol,
                        Timeout = plc.Timeout
                    };
                    break;
                default:
                    _boolTag = new Tag<CustomBoolPlcMapper, bool>()
                    {
                        Name = tag,
                        Gateway = plc.IPAddress,
                        Path = plc.Route,
                        PlcType = plc.PLC_Type,
                        Protocol = plc.Protocol,
                        Timeout = plc.Timeout
                    };
                    break;
            }
            _active = false;
            _type = type;
            TagName = tag;
            
        }

        public object GetValue()
        {
            switch (_type)
            {
                case TAGType.Bool:
                    return _boolTag.Value;
                case TAGType.Int:
                    return _intTag.Value;
                case TAGType.Dint:
                    return _dintTag.Value;
                case TAGType.Real:
                    return _realTag.Value;
                case TAGType.String:
                    return _stringTag.Value;
                default:
                    return 0;
            }
        }

        public void SetValue(object value)
        {
            switch (_type)
            {
                case TAGType.Bool:
                    _boolTag.Value = (bool)value;
                    break;
                case TAGType.Int:
                    _intTag.Value = (short)value;
                    break;
                case TAGType.Dint:
                    _dintTag.Value = (int)value;
                    break;
                case TAGType.Real:
                    _realTag.Value = (float)value;
                    break;
                case TAGType.String:
                    _stringTag.Value = (string)value;
                    break;
                default:
                    break;
            }
        }

        public object Read()
        {
            switch (_type)
            {
                case TAGType.Bool:
                    _boolTag.Read();
                    break;
                case TAGType.Int:
                    _intTag.Read();
                    break;
                case TAGType.Dint:
                    _dintTag.Read();
                    break;
                case TAGType.Real:
                    _realTag.Read();
                    break;
                case TAGType.String:
                    _stringTag.Read();
                    break;
                default:
                    break;
            }
            return GetValue();
        }

        public void Write(object value)
        {
            switch (_type)
            {
                case TAGType.Bool:
                    SetValue(value);
                    _boolTag.Write();
                    break;
                case TAGType.Int:
                    SetValue(value);
                    _intTag.Write();
                    break;
                case TAGType.Dint:
                    SetValue(value);
                    _dintTag.Write();
                    break;
                case TAGType.Real:
                    SetValue(value);
                    _realTag.Write();
                    break;
                case TAGType.String:
                    SetValue(value);
                    _stringTag.Write();
                    break;
                default:
                    break;
            }
        }



        private void ActivateUpdateTimer(bool enable)
        {
            if (UpdateCycle == null)
            {
                UpdateCycle = new System.Timers.Timer();
            }
            if (enable)
            {
                UpdateCycle.Enabled = true;
                UpdateCycle.Interval = UpdateTime_ms;
                UpdateCycle.Elapsed += UpdateTimer_Tick;
                UpdateCycle.Start();
            }
            else
            {
                UpdateCycle.Enabled = false;
                UpdateCycle.Stop();
                UpdateCycle.Elapsed -= UpdateTimer_Tick;

            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            Read();

            switch (_type)
            {
                case TAGType.Bool:
                    if (_boolPrevious != _boolTag.Value)
                    {
                        _boolPrevious = (bool)GetValue();
                        DataChanged.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case TAGType.Int:
                    if (_intPrevious != _intTag.Value)
                    {
                        _intPrevious = (short)GetValue();
                        DataChanged.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case TAGType.Dint:
                    if (_dintPrevious != _dintTag.Value)
                    {
                        _dintPrevious = (int)GetValue();
                        DataChanged.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case TAGType.Real:
                    if (_realPrevious != _realTag.Value)
                    {
                        _realPrevious = (float)GetValue();
                        DataChanged.Invoke(this, EventArgs.Empty);
                    }
                    break;
                case TAGType.String:
                    if (_stringPrevious != _stringTag.Value)
                    {
                        _stringPrevious = (string)GetValue();
                        DataChanged.Invoke(this, EventArgs.Empty);
                    }
                    break;
                default:
                    break;
            }
        }


    }

    public enum TAGType
    {
        Bool,
        Int,
        Dint,
        Real,
        String
    }

    public class ParentPLCData
    {
        public string? Name { get; set; }
        public string? IPAddress { get; set; }
        public string? Route { get; set; }
        public PlcType PLC_Type = PlcType.ControlLogix;
        public Protocol Protocol = Protocol.ab_eip;
        public TimeSpan Timeout = TimeSpan.FromSeconds(5);
    }

    public class CustomBoolPlcMapper : IPlcMapper<bool>
    {
        public int? ElementSize => 1;

        public PlcType PlcType { get; set; }
        public int[]? ArrayDimensions { get; set; }

        public int? GetElementCount() => 1;

        bool IPlcMapper<bool>.Decode(Tag tag) => tag.GetUInt8(0) == 1;

        void IPlcMapper<bool>.Encode(Tag tag, bool value) => tag.SetUInt8(0, value == true ? (byte)1 : (byte)0);
    }
