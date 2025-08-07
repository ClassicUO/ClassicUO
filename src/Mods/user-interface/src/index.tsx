import React from 'react';
import { LoginScreen, StorybookScreen } from './components';
import { HostMessages, Keys, TermOp, TimeProxy, UIEvent } from './host';
import { HostWrapper } from './host/wrapper';
import { ClayReactRenderer } from './react';
import { State } from './state';
import { polyfillGlobals } from './support/polyfill';

// Main plugin functions
export function on_init(): I32 {
  const polyfills = polyfillGlobals();

  State.init(polyfills);

  console.log('plugin initialized');

  return 1;
}

export function on_update(): I32 {
  try {
    const json = HostWrapper.getInputString();
    const time: TimeProxy = JSON.parse(json);

    State.update(time);
  } catch (e) {
    console.error('error during on_update', e);
  }

  return 1;
}

export function on_event(): I32 {
  const json = HostWrapper.getInputString();
  const evList: HostMessages = JSON.parse(json);

  for (const ev of evList.messages) {
    switch (ev.$type) {
      case 'KeyPressed':
        break;

      case 'KeyReleased':
        switch (ev.key) {
          // Login Scenex
          case Keys.R:
            State.renderer?.unmount();
            State.renderer = new ClayReactRenderer();

            State.renderer.render(
              <LoginScreen
                onQuit={() => console.log('React: Quit button clicked')}
                onCredits={() => console.log('React: Credits button clicked')}
                onLogin={(username, password) => {
                    console.log('React: Login button clicked');
                    HostWrapper.sendEvents({
                        messages: [{
                          $type: 'LoginRequest',
                          username: username,
                          password: password
                        }]
                    })
                  }
                }
              />,
            );
            break;

          // Storybook
          case Keys.T:
            State.renderer?.unmount();
            State.renderer = new ClayReactRenderer();

            State.renderer.render(<StorybookScreen />);
            break;

          case Keys.H:
            const response = HostWrapper.query({
              terms: [{ ids: 1, op: TermOp.Optional }],
            });
            for (const result of response.results) {
              console.log('ecs query result', result);
            }
            break;
        }
        break;
    }
  }

  return 1;
}

export function on_ui_event(): I32 {
  const json = HostWrapper.getInputString();
  const ev: UIEvent = JSON.parse(json);

  State.renderer?.handleUIEvent(ev);
  return 1;
}
