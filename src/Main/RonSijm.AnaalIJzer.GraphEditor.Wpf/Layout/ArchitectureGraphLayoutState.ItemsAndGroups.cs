using System.Windows;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Layout;

public sealed partial class ArchitectureGraphLayoutState
{
	public Point GetLocation(string path, Point fallback)
	{
		var result = _items.TryGetValue(path, out var item) && item.Location is not null ? item.Location.Value : fallback;

		return result;
	}

	public Size GetSize(string path, Size fallback)
	{
		var result = _items.TryGetValue(path, out var item) && item.Size is not null ? item.Size.Value : fallback;

		return result;
	}

	public void SetLocation(string path, Point location)
	{
		var item = GetOrCreate(path);
		if (item.Location == location)
		{
			return;
		}

		item.Location = location;
		_isDirty = true;
	}

	public void SetSize(string path, Size size)
	{
		var item = GetOrCreate(path);
		if (item.Size == size)
		{
			return;
		}

		item.Size = size;
		_isDirty = true;
	}

	public double GetGroupHeight(string key, double fallback)
	{
		var result = _groups.TryGetValue(key, out var group) && group.Height is not null ? group.Height.Value : fallback;

		return result;
	}

	public bool GetGroupIsCollapsed(string key, bool fallback)
	{
		var result = _groups.TryGetValue(key, out var group) && group.IsCollapsed is not null ? group.IsCollapsed.Value : fallback;

		return result;
	}

	public void SetGroupHeight(string key, double height)
	{
		if (!IsUsableDimension(height))
		{
			return;
		}

		var group = GetOrCreateGroup(key);
		if (group.Height == height)
		{
			return;
		}

		group.Height = height;
		_isDirty = true;
	}

	public void SetGroupIsCollapsed(string key, bool isCollapsed)
	{
		var group = GetOrCreateGroup(key);
		if (group.IsCollapsed == isCollapsed)
		{
			return;
		}

		group.IsCollapsed = isCollapsed;
		_isDirty = true;
	}

	private GraphItemLayout GetOrCreate(string path)
	{
		if (_items.TryGetValue(path, out var item))
		{
			return item;
		}

		var result = new GraphItemLayout(path);
		_items.Add(path, result);

		return result;
	}

	private GraphGroupLayout GetOrCreateGroup(string key)
	{
		if (_groups.TryGetValue(key, out var group))
		{
			return group;
		}

		var result = new GraphGroupLayout(key);
		_groups.Add(key, result);

		return result;
	}
}
