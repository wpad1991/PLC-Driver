using System;
using System.Collections.Generic;
using System.Text;

namespace DriverInterface.Configuration
{
    public class SerialConfig : DriverInformation
    {
        public string portName;
        public int baudRate;
        /// <summary>
        ///
        /// 요약: 0,
        ///     패리티 검사가 수행 되지 않습니다.
        ///
        /// 요약: 1,
        ///     비트 집합의 횟수가 홀수 있도록 패리티 비트를 설정 합니다.
        ///
        /// 요약: 2,
        ///     비트 집합의 수는 짝수 있도록 패리티 비트를 설정 합니다.
        ///
        /// 요약: 3,
        ///     패리티 비트를 1로 설정 그대로 둡니다.
        ///
        /// 요약: 4,
        ///     패리티 비트를 0으로 설정 그대로 둡니다.
        /// </summary>
        public int parity;
        public int dataBits;
        /// <summary>
        /// 
        /// 요약: 0,
        ///     없음 정지 비트를 사용 합니다. 이 값에서 지원 하지 않는 System.IO.Ports.SerialPort.StopBits 속성입니다.
        ///
        /// 요약: 1,
        ///     정지 비트 사용 됩니다.
        ///
        /// 요약: 2,
        ///     두 개의 정지 비트를 사용 합니다.
        ///
        /// 요약: 3,
        ///     1.5 정지 비트를 사용 합니다.
        /// </summary>
        public int stopBits;
    }
    
}
