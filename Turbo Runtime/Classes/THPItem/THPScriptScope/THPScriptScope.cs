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
using System.Linq;
using System.Security.Permissions;

namespace Turbo.Runtime
{
    internal sealed class THPScriptScope : THPItem, ITHPScriptScope, IDebugScriptScope
    {
        private THPScriptScope parent;

        private GlobalScope scope;

        private ArrayList items;

        private bool isCompiled;

        private bool isClosed;

        public ITHPScriptScope Parent => parent;

        internal THPScriptScope(THPMainEngine engine, string itemName, THPScriptScope parent)
            : base(engine, itemName, (ETHPItemType) 19, ETHPItemFlag.None)
        {
            this.parent = parent;
            scope = null;
            items = new ArrayList(8);
            isCompiled = false;
            isClosed = false;
        }

        public object GetObject()
            => scope
               ?? (scope = parent != null
                   ? new GlobalScope((GlobalScope) parent.GetObject(), engine, false)
                   : new GlobalScope(null, engine));

        public ITHPItem AddItem(string itemName, ETHPItemType type)
        {
            THPItem thpItem = null;
            if (isClosed)
            {
                throw new THPException(ETHPError.EngineClosed);
            }
            if (GetItem(itemName) != null)
            {
                throw new THPException(ETHPError.ItemNameInUse);
            }
            switch (type)
            {
                case (ETHPItemType) 16:
                case (ETHPItemType) 17:
                case (ETHPItemType) 18:
                    thpItem = new THPHostObject(engine, itemName, type, this);
                    if (type == (ETHPItemType) 17 || type == (ETHPItemType) 18)
                    {
                        ((THPHostObject) thpItem).exposeMembers = true;
                    }
                    if (type == (ETHPItemType) 16 || type == (ETHPItemType) 18)
                    {
                        ((THPHostObject) thpItem).isVisible = true;
                    }
                    if (engine.IsRunning)
                    {
                        ((THPHostObject) thpItem).Compile();
                        ((THPHostObject) thpItem).Run();
                    }
                    break;
                case (ETHPItemType) 19:
                    thpItem = new THPScriptScope(engine, itemName, this);
                    break;
                case (ETHPItemType) 20:
                    thpItem = new THPScriptCode(engine, itemName, type, this);
                    break;
                case (ETHPItemType) 21:
                    if (!engine.IsRunning)
                    {
                        throw new THPException(ETHPError.EngineNotRunning);
                    }
                    thpItem = new THPScriptCode(engine, itemName, type, this);
                    break;
                case (ETHPItemType) 22:
                    if (!engine.IsRunning)
                    {
                        throw new THPException(ETHPError.EngineNotRunning);
                    }
                    thpItem = new THPScriptCode(engine, itemName, type, this);
                    break;
            }
            if (thpItem == null) throw new THPException(ETHPError.ItemTypeNotSupported);
            items.Add(thpItem);
            return thpItem;
        }

        public ITHPItem GetItem(string itemName)
        {
            var i = 0;
            var count = items.Count;
            while (i < count)
            {
                var thpItem = (THPItem) items[i];
                if ((thpItem.Name == null && itemName == null) ||
                    (thpItem.Name != null && thpItem.Name.Equals(itemName)))
                {
                    return (ITHPItem) items[i];
                }
                i++;
            }
            return null;
        }

        public void RemoveItem(string itemName)
        {
            var i = 0;
            var count = items.Count;
            while (i < count)
            {
                var thpItem = (THPItem) items[i];
                if ((thpItem.Name == null && itemName == null) ||
                    (thpItem.Name != null && thpItem.Name.Equals(itemName)))
                {
                    thpItem.Remove();
                    items.Remove(i);
                    return;
                }
                i++;
            }
            throw new THPException(ETHPError.ItemNotFound);
        }

        public void RemoveItem(ITHPItem item)
        {
            var i = 0;
            var count = items.Count;
            while (i < count)
            {
                var thpItem = (THPItem) items[i];
                if (thpItem == item)
                {
                    thpItem.Remove();
                    items.Remove(i);
                    return;
                }
                i++;
            }
            throw new THPException(ETHPError.ItemNotFound);
        }

        public int GetItemCount() => items.Count;

        public ITHPItem GetItemAtIndex(int index)
        {
            if (index < items.Count)
            {
                return (ITHPItem) items[index];
            }
            throw new THPException(ETHPError.ItemNotFound);
        }

        public void RemoveItemAtIndex(int index)
        {
            if (index >= items.Count) throw new THPException(ETHPError.ItemNotFound);
            ((THPItem) items[index]).Remove();
            items.Remove(index);
        }

        public ITHPItem CreateDynamicItem(string itemName, ETHPItemType type)
        {
            if (engine.IsRunning)
            {
                return AddItem(itemName, type);
            }
            throw new THPException(ETHPError.EngineNotRunning);
        }

        internal override void CheckForErrors()
        {
            if (items.Count == 0)
            {
                return;
            }
            try
            {
                engine.Globals.ScopeStack.Push((ScriptObject) GetObject());
                var enumerator = items.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        ((THPItem) enumerator.Current).CheckForErrors();
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
            }
            finally
            {
                engine.Globals.ScopeStack.Pop();
            }
        }

        internal override void Compile()
        {
            if (items.Count == 0)
            {
                return;
            }
            if (isCompiled) return;
            isCompiled = true;
            try
            {
                engine.Globals.ScopeStack.Push((ScriptObject) GetObject());
                try
                {
                    var enumerator = items.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            ((THPItem) enumerator.Current).Compile();
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
                }
                finally
                {
                    engine.Globals.ScopeStack.Pop();
                }
            }
            catch
            {
                isCompiled = false;
                throw;
            }
        }

        internal override void Reset()
        {
            var enumerator = items.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    ((THPItem) enumerator.Current).Reset();
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
        }

        internal void ReRun(GlobalScope scope)
        {
            foreach (var current in items.OfType<THPHostObject>())
            {
                (current).ReRun(scope);
            }
            if (parent != null)
            {
                parent.ReRun(scope);
            }
        }

        internal override void Run()
        {
            if (items.Count == 0)
            {
                return;
            }
            try
            {
                engine.Globals.ScopeStack.Push((ScriptObject) GetObject());
                var enumerator = items.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        ((THPItem) enumerator.Current).Run();
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
            }
            finally
            {
                engine.Globals.ScopeStack.Pop();
            }
        }

        internal override void Close()
        {
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
            parent = null;
            scope = null;
            isClosed = true;
        }

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust")]
        public void SetThisValue(object thisValue)
        {
            if (scope != null)
            {
                scope.thisObject = thisValue;
            }
        }
    }
}