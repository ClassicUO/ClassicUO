import React from "react";
import {
  View,
  Gump,
  Button,
  TextInput,
  Colors,
  Sizing,
  Layout,
} from "../react";
import {
  ClayFloatingAttachToElement,
  ClayFloatingClipToElement,
  ClayLayoutAlignment,
  ClayLayoutDirection,
  ClaySizingType,
} from "src/types";

export interface LoginScreenProps {
  onQuit: () => void;
  onCredits: () => void;
  onLogin: () => void;
}

export const LoginScreen: React.FC<LoginScreenProps> = ({
  onQuit,
  onCredits,
  onLogin,
}) => {
  console.log("LoginScreen", { useState: typeof React.useState });
  const [username, setUsername] = React.useState("Your Username");
  const [password, setPassword] = React.useState("");
  const [showCredits, setShowCredits] = React.useState(true);

  console.log("LoginScreen showCredits", showCredits);

  return (
    <View
      backgroundColor={Colors.darkGray}
      layout={{
        sizing: {
          width: {
            type: ClaySizingType.Grow,
            size: {},
          },
          height: {
            type: ClaySizingType.Grow,
            size: {},
          },
        },
        childAlignment: {
          x: ClayLayoutAlignment.Center,
          y: ClayLayoutAlignment.Center,
        },
        layoutDirection: ClayLayoutDirection.TopToBottom,
      }}
    >
      <Gump
        gumpId={334}
        hue={{ z: 1 }}
        // ninePatch={true}
        size={{ width: 640, height: 480 }}
        direction={ClayLayoutDirection.TopToBottom}
      >
        <Button
          onClick={onQuit}
          gumpIds={{ normal: 0x05ca, pressed: 0x05c9, over: 0x05c8 }}
          size={{ width: 66, height: 66 }}
          hue={{ x: 0, y: 0, z: 1 }}
          floating={{
            offset: { x: 25, y: 240 },
            attachTo: ClayFloatingAttachToElement.Parent,
            clipTo: ClayFloatingClipToElement.AttachedParent,
          }}
        />
        {showCredits && (
          <Button
            onClick={onCredits}
            gumpIds={{ normal: 0x05d0, pressed: 0x05cf, over: 0x05ce }}
            size={{ width: 104, height: 36 }}
            hue={{ x: 0, y: 0, z: 1 }}
            floating={{
              offset: { x: 530, y: 125 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
          />
        )}

        <Button
          gumpIds={{ normal: 0x05cd, pressed: 0x05cc, over: 0x05cb }}
          size={{ width: 85, height: 28 }}
          hue={{ x: 0, y: 0, z: 1 }}
          floating={{
            offset: { x: 280, y: 365 },
            attachTo: ClayFloatingAttachToElement.Parent,
            clipTo: ClayFloatingClipToElement.AttachedParent,
          }}
          onClick={() => {
            setShowCredits((prev) => !prev);
            onLogin();
          }}
        />

        <Gump
          gumpId={0x0bb8}
          hue={{ x: 0, y: 0, z: 1 }}
          ninePatch={true}
          size={{ width: 210, height: 30 }}
          floating={{
            offset: { x: 218, y: 283 },
            attachTo: ClayFloatingAttachToElement.Parent,
            clipTo: ClayFloatingClipToElement.AttachedParent,
          }}
          childAlignment={{
            x: ClayLayoutAlignment.Center,
            y: ClayLayoutAlignment.Center,
          }}
          direction={ClayLayoutDirection.LeftToRight}
        >
          <TextInput
            placeholder="your username"
            textStyle={{
              fontId: 0,
              fontSize: 24,
              textColor: Colors.darkGray,
            }}
            value={username}
            onChange={(value) => setUsername(value)}
            size={{ width: 210, height: 30 }}
          />
        </Gump>
        <Gump
          gumpId={0x0bb8}
          hue={{ x: 0, y: 0, z: 1 }}
          ninePatch={true}
          size={{ width: 210, height: 30 }}
          floating={{
            offset: { x: 218, y: 333 }, // 283 + 50
            attachTo: ClayFloatingAttachToElement.Parent,
            clipTo: ClayFloatingClipToElement.AttachedParent,
          }}
          direction={ClayLayoutDirection.LeftToRight}
          childAlignment={{
            x: ClayLayoutAlignment.Center,
            y: ClayLayoutAlignment.Center,
          }}
        >
          <TextInput
            placeholder="your password"
            password={true}
            textStyle={{
              fontId: 0,
              fontSize: 24,
              textColor: Colors.white,
            }}
            value={password}
            onChange={(value) => setPassword(value)}
            size={{ width: 210, height: 30 }}
          />
        </Gump>
      </Gump>
    </View>
  );
};
