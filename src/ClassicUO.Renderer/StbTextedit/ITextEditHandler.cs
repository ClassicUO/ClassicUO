// SPDX-License-Identifier: BSD-2-Clause

namespace StbTextEditSharp
{
    public interface ITextEditHandler
    {
        string Text { get; set; }

        int Length { get; }

        TextEditRow LayoutRow(int startIndex);

        float GetWidth(int index);
    }
}