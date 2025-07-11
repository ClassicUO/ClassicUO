# Design System Storybook

Showcases design system components with interactive examples.

## Structure

```
storybook/
├── StorybookScreen.tsx      # Main storybook screen
├── StorybookContainer.tsx   # UO-authentic container
├── StorybookSection.tsx     # Component grouping sections
├── ComponentShowcase.tsx    # Individual component demos
├── stories/                 # Component stories
│   ├── IconButtonStory.tsx  # IconButton examples
│   └── index.ts            # Story exports
└── index.ts                # Main exports
```

## Usage

```tsx
import { StorybookScreen } from "~/components/scenes/storybook";

<StorybookScreen onBack={() => console.log("Back clicked")} />
```

## Adding New Component Stories

1. Create story file in `stories/`:

```tsx
// stories/MyComponentStory.tsx
import React from "react";
import { MyComponent } from "~/components/design-system";
import { StorybookSection } from "../StorybookSection";
import { ComponentShowcase } from "../ComponentShowcase";

export const MyComponentStory: React.FC = () => {
  return (
    <StorybookSection title="MyComponent" description="Component description">
      <ComponentShowcase
        name="Basic Usage"
        description="Simple example"
        code="<MyComponent prop={value} />"
      >
        <MyComponent prop={value} />
      </ComponentShowcase>
    </StorybookSection>
  );
};
```

2. Export in `stories/index.ts`:
```tsx
export { MyComponentStory } from "./MyComponentStory";
```

3. Add to `StorybookScreen.tsx`:
```tsx
import { MyComponentStory } from "./stories";
<MyComponentStory />
```

## Components

- **StorybookContainer**: Main container with UO gump background
- **StorybookSection**: Groups related components with title/description
- **ComponentShowcase**: Individual component demo with code examples

## Requirements

- Use only reconciler primitives (Button, Gump, Art, View, Text)
- Follow UO gump IDs and design patterns
- Include TypeScript types and JSDoc comments
- Add interactive examples to storybook