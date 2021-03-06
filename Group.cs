// (C) 2012 Christian Schladetsch. See http://www.schladetsch.net/flow/license.txt for Licensing information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Flow.Impl
{
	/// <summary>
	///     A flow Group contains a collection of other Transients, and fires events when the contents
	///     of the group changes.
	///     Suspending a Group suspends all contained Generators, and Resuming a Group
	///     Resumes all contained Generators.
	///		Stepping a group does nothing.
	/// </summary>
	internal class Group : Generator<bool>, IGroup
	{
		public event GroupHandler Added;
		public event GroupHandler Removed;

		internal Group()
		{
			Resumed += tr => ForEachGenerator(g => g.Resume());
			Suspended += tr => ForEachGenerator(g => g.Suspend());
			Completed += tr => Clear();
		}

		public IEnumerable<IGenerator> Generators
		{
			get { return Contents.OfType<IGenerator>(); }
		}

		public IEnumerable<ITransient> Contents
		{
			get { return _contents; }
		}

		public override void Post()
		{
			PerformPending();
		}

		public void Add(params ITransient[] others)
		{
			foreach (var other in others)
			{
				if (other == null)
					continue;

				Deletions.RemoveRef(other);
				Additions.Add(other);
			}
		}

		public void Remove(ITransient other)
		{
			if (other == null)
				return;

			Additions.RemoveRef(other);
			Deletions.Add(other);
		}

		public void Clear()
		{
			Additions.Clear();

			foreach (var tr in Contents)
			{
				Deletions.Add(tr);
			}

			PerformRemoves();
		}

		private void ForEachGenerator(Action<IGenerator> act)
		{
			foreach (var gen in Generators)
			{
				act(gen);
			}
		}

		protected void PerformPending()
		{
			PerformAdds();
			PerformRemoves();
		}

		private void PerformRemoves()
		{
			if (Deletions.Count == 0)
				return;
			
			foreach (var tr in Deletions.ToList())
			{
				_contents.RemoveRef(tr);
				if (tr == null)
					continue;

				tr.Completed -= Remove;

				if (Removed != null)
					Removed(this, tr);
			}

			Deletions.Clear();
		}

		private void PerformAdds()
		{
			foreach (var tr in Additions)
			{
				_contents.Add(tr);

				tr.Completed += Remove;

				if (Added != null)
					Added(this, tr);
			}

			Additions.Clear();
		}

		protected readonly List<ITransient> Additions = new List<ITransient>();
		protected readonly List<ITransient> Deletions = new List<ITransient>();
		protected readonly List<ITransient> _contents = new List<ITransient>();

	}
}
