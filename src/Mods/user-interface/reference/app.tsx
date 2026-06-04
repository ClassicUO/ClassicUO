// Ground-truth reference page: renders the REAL design-system components through
// react-dom (via the ~/react -> clay-dom shim). Hover the icons to exercise the
// Tooltip timer/visibility logic with real, non-bubbling DOM mouseenter/leave —
// the behaviour the cuo-ecs mod should match. Compare against an agent-harness
// shot of the mod.

import * as React from 'react';
import { createRoot } from 'react-dom/client';
import { IconButton } from '~/components/design-system/IconButton';
import { Tooltip } from '~/components/design-system/Tooltip';

const LONG =
  'This is a long tooltip describing the action in detail. It must wrap onto ' +
  'several lines inside the tooltip box instead of running off the right edge.';

function Scenario({ id, label, children }: { id: string; label: string; children: React.ReactNode }) {
  return (
    <div id={id} style={{ display: 'flex', flexDirection: 'column', gap: 6, alignItems: 'center' }}>
      <div style={{ fontSize: 11, color: '#9ab' }}>{label}</div>
      {children}
    </div>
  );
}

function App() {
  const [log, setLog] = React.useState<string[]>([]);
  const note = (m: string) => setLog((l) => [...l.slice(-6), m]);

  const icon = { type: 'art', id: 0x1234 } as const;
  const size = { width: 44, height: 44 };

  return (
    <div className="panel">
      <h2>Tooltip — hover behaviour (delay) </h2>
      <div className="row" style={{ marginTop: 60 }}>
        <Scenario id="tip-instant" label="instant (delay 0)">
          <Tooltip content="Instant tip" delay={0}>
            <IconButton icon={icon} size={size} onClick={() => note('click instant')} />
          </Tooltip>
        </Scenario>

        <Scenario id="tip-delayed" label="delayed (delay 500)">
          <Tooltip content="Delayed tip" delay={500}>
            <IconButton icon={icon} size={size} onClick={() => note('click delayed')} />
          </Tooltip>
        </Scenario>

        <Scenario id="tip-bottom" label="position bottom">
          <Tooltip content="Below the icon" delay={300} position="bottom">
            <IconButton icon={icon} size={size} />
          </Tooltip>
        </Scenario>
      </div>

      <h2>Tooltip — text wrapping</h2>
      <div className="row" style={{ marginTop: 120 }}>
        <Scenario id="tip-wrap" label="long text wraps in box (width 300)">
          <Tooltip content={LONG} delay={0} position="bottom">
            <IconButton icon={icon} size={size} />
          </Tooltip>
        </Scenario>
      </div>

      <div style={{ position: 'fixed', right: 12, bottom: 12, fontSize: 11, color: '#7a7' }}>
        log: {log.join(' | ')}
      </div>
    </div>
  );
}

createRoot(document.getElementById('root')!).render(<App />);
