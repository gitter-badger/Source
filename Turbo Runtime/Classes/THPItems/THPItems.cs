#region LICENSE

/*
 * This  license  governs  use  of  the accompanying software. If you use the software, you accept this
 * license. If you do not accept the license, do not use the software.
 *
 * 1. Definitions
 *
 * The terms "reproduce", "reproduction", "derivative works",  and "distribution" have the same meaning
 * here as under U.S.  copyright law.  A " contribution"  is the original software, or any additions or
 * changes to the software.  A "contributor" is any person that distributes its contribution under this
 * license.  "Licensed patents" are contributor's patent claims that read directly on its contribution.
 *
 * 2. Grant of Rights
 *
 * (A) Copyright  Grant-  Subject  to  the  terms of this license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * copyright license to reproduce its contribution,  prepare derivative works of its contribution,  and
 * distribute its contribution or any derivative works that you create.
 *
 * (B) Patent  Grant-  Subject  to  the  terms  of  this  license, including the license conditions and
 * limitations in section 3,  each  contributor  grants you a  non-exclusive,  worldwide,  royalty-free
 * license under its licensed patents to make,  have made,  use,  sell,  offer for sale, import, and/or
 * otherwise dispose of its contribution in the software or derivative works of the contribution in the
 * software.
 *
 * 3. Conditions and Limitations
 *
 * (A) Reciprocal Grants-  For any file you distribute that contains code from the software  (in source
 * code or binary format),  you must provide  recipients a copy of this license.  You may license other
 * files that are  entirely your own work and do not contain code from the software under any terms you
 * choose.
 *
 * (B) No Trademark License- This license does not grant you rights to use a contributors'  name, logo,
 * or trademarks.
 *
 * (C) If you bring a patent claim against any contributor over patents that you claim are infringed by
 * the software, your patent license from such contributor to the software ends automatically.
 *
 * (D) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
 * and attribution notices that are present in the software.
 *
 * (E) If you distribute any portion of the software in source code form, you may do so while including
 * a complete copy of this license with your distribution.
 *
 * (F) The software is licensed as-is. You bear the risk of using it.  The contributors give no express
 * warranties, guarantees or conditions.  You may have additional consumer rights under your local laws
 * which this license cannot change.  To the extent permitted under  your local laws,  the contributors
 * exclude  the  implied  warranties  of  merchantability,  fitness  for  a particular purpose and non-
 * infringement.
 */

#endregion

using System;
using System.Collections;
using System.Security.Permissions;

namespace Turbo.Runtime
{
    public sealed class THPItems : ITHPItems
    {
        private ArrayList items;

        private bool isClosed;

        private THPMainEngine engine;

        internal int staticCodeBlockCount;

        public ITHPItem this[int index]
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                if (isClosed)
                {
                    throw new THPException(ETHPError.EngineClosed);
                }
                if (index < 0 || index >= items.Count)
                {
                    throw new THPException(ETHPError.ItemNotFound);
                }
                return (ITHPItem) items[index];
            }
        }

        public ITHPItem this[string itemName]
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                if (isClosed)
                {
                    throw new THPException(ETHPError.EngineClosed);
                }
                if (itemName == null) throw new THPException(ETHPError.ItemNotFound);
                var i = 0;
                var count = items.Count;
                while (i < count)
                {
                    var iJSVsaItem = (ITHPItem) items[i];
                    if (iJSVsaItem.Name.Equals(itemName))
                    {
                        return iJSVsaItem;
                    }
                    i++;
                }
                throw new THPException(ETHPError.ItemNotFound);
            }
        }

        public int Count
        {
            [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
            get
            {
                if (isClosed)
                {
                    throw new THPException(ETHPError.EngineClosed);
                }
                return items?.Count ?? 0;
            }
        }

        public THPItems(THPMainEngine engine)
        {
            this.engine = engine;
            staticCodeBlockCount = 0;
            items = new ArrayList(10);
        }

        public void Close()
        {
            if (isClosed)
            {
                throw new THPException(ETHPError.EngineClosed);
            }
            TryObtainLock();
            try
            {
                isClosed = true;
                var enumerator = items.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        ((THPItem) enumerator.Current).Close();
                    }
                }
                finally
                {
                    var disposable = enumerator as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
                items = null;
            }
            finally
            {
                ReleaseLock();
                engine = null;
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public ITHPItem CreateItem(string name, ETHPItemType itemType, ETHPItemFlag itemFlag)
        {
            if (isClosed)
            {
                throw new THPException(ETHPError.EngineClosed);
            }
            if (engine.IsRunning)
            {
                throw new THPException(ETHPError.EngineRunning);
            }
            TryObtainLock();
            ITHPItem result;
            try
            {
                if (itemType != ETHPItemType.Reference && !engine.IsValidIdentifier(name))
                {
                    throw new THPException(ETHPError.ItemNameInvalid);
                }
                var enumerator = items.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        if (((THPItem) enumerator.Current).Name.Equals(name))
                        {
                            throw new THPException(ETHPError.ItemNameInUse);
                        }
                    }
                }
                finally
                {
                    var disposable = enumerator as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
                ITHPItem iJSVsaItem = null;
                switch (itemType)
                {
                    case ETHPItemType.Reference:
                        if (itemFlag != ETHPItemFlag.None)
                        {
                            throw new THPException(ETHPError.ItemFlagNotSupported);
                        }
                        iJSVsaItem = new THPReference(engine, name);
                        break;
                    case ETHPItemType.AppGlobal:
                        if (itemFlag != ETHPItemFlag.None)
                        {
                            throw new THPException(ETHPError.ItemFlagNotSupported);
                        }
                        iJSVsaItem = new THPHostObject(engine, name, ETHPItemType.AppGlobal);
                        ((THPHostObject) iJSVsaItem).isVisible = true;
                        break;
                    case ETHPItemType.Code:
                        if (itemFlag == ETHPItemFlag.Class)
                        {
                            throw new THPException(ETHPError.ItemFlagNotSupported);
                        }
                        iJSVsaItem = new THPStaticCode(engine, name, itemFlag);
                        staticCodeBlockCount++;
                        break;
                }
                if (iJSVsaItem == null)
                {
                    throw new THPException(ETHPError.ItemTypeNotSupported);
                }
                items.Add(iJSVsaItem);
                engine.IsDirty = true;
                result = iJSVsaItem;
            }
            finally
            {
                ReleaseLock();
            }
            return result;
        }

        public IEnumerator GetEnumerator()
        {
            if (isClosed)
            {
                throw new THPException(ETHPError.EngineClosed);
            }
            return items.GetEnumerator();
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void Remove(string itemName)
        {
            if (isClosed)
            {
                throw new THPException(ETHPError.EngineClosed);
            }
            TryObtainLock();
            try
            {
                if (itemName == null)
                {
                    throw new ArgumentNullException(nameof(itemName));
                }
                var i = 0;
                var count = items.Count;
                while (i < count)
                {
                    var iJSVsaItem = (ITHPItem) items[i];
                    if (iJSVsaItem.Name.Equals(itemName))
                    {
                        ((THPItem) iJSVsaItem).Remove();
                        items.RemoveAt(i);
                        engine.IsDirty = true;
                        if (iJSVsaItem is THPStaticCode)
                        {
                            staticCodeBlockCount--;
                        }
                        return;
                    }
                    i++;
                }
                throw new THPException(ETHPError.ItemNotFound);
            }
            finally
            {
                ReleaseLock();
            }
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void Remove(int itemIndex)
        {
            if (isClosed)
            {
                throw new THPException(ETHPError.EngineClosed);
            }
            TryObtainLock();
            try
            {
                if (0 > itemIndex || itemIndex >= items.Count)
                {
                    throw new THPException(ETHPError.ItemNotFound);
                }
                var expr_3C = (THPItem) items[itemIndex];
                expr_3C.Remove();
                items.RemoveAt(itemIndex);
                if (expr_3C is THPStaticCode)
                {
                    staticCodeBlockCount--;
                }
            }
            finally
            {
                ReleaseLock();
            }
        }

        private void TryObtainLock()
        {
            engine.TryObtainLock();
        }

        private void ReleaseLock()
        {
            engine.ReleaseLock();
        }
    }
}