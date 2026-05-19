using ProyectMVP.DeviceAgent.Models;

namespace ProyectMVP.DeviceAgent.Lpr;

public interface ILprReader
{
    LprReading Read();
}
