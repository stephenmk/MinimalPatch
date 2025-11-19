/*
Copyright (c) 2025 Stephen Kraus

This file is part of MinimalPatch.

MinimalPatch is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MinimalPatch is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
General Public License for more details.

You should have received a copy of the GNU General Public License
along with MinimalPatch. If not, see <https://www.gnu.org/licenses/>.
*/

namespace MinimalPatch.Test;

[TestClass]
public sealed class PatchTest
{
    [TestMethod]
    public void PatchApplyTest1()
    {
        var diff = File.ReadAllText(Path.Join("Data", "hamlet_ending1.patch"));
        var originalText = File.ReadAllText(Path.Join("Data", "hamlet_ending_old.txt"));
        var expectedText = File.ReadAllText(Path.Join("Data", "hamlet_ending_new.txt"));
        var newText = Patch.Apply(diff, originalText);
        Assert.AreEqual(expectedText, newText);
    }

    [TestMethod]
    public void PatchApplyTest2()
    {
        var diff = File.ReadAllText(Path.Join("Data", "hamlet_ending2.patch"));
        var originalText = File.ReadAllText(Path.Join("Data", "hamlet_ending_old.txt"));
        var expectedText = File.ReadAllText(Path.Join("Data", "hamlet_ending_new.txt"));
        var newText = Patch.Apply(diff, originalText);
        Assert.AreEqual(expectedText, newText);
    }
}
