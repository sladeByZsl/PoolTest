using UnityEngine;
using System.Collections;

namespace ELEX.NewPool
{
    public interface IPoolItem
    {
        /** 对象池的名称描述,没啥用，纯粹描述信息 */
        string PName{ get; set;}

        /** 销毁 */
        void PDestruct();

        /** 对象池设置--该对象是否激活 */
        void PSetActive(bool value);

        /** 对象池设置--该对象重设参数 */
        void PSetArg(params object[] args);

    }
}