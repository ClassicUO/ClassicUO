import React from "react";
import {
  View,
  Gump,
  Button,
  TextInput,
  Checkbox,
  Label,
  HSliderBar,
  Colors,
} from "../react";
import {
  ClayFloatingAttachToElement,
  ClayFloatingClipToElement,
  ClayLayoutAlignment,
  ClayLayoutDirection,
  ClaySizingType,
} from "src/types";
import { LoginBackground } from "./LoginBackground";

export interface LoginScreenProps {
  onQuit: () => void;
  onCredits: () => void;
  onLogin: (username: string, password: string) => void;
}

export const LoginScreen: React.FC<LoginScreenProps> = ({
  onQuit,
  onCredits,
  onLogin,
}) => {
  const [username, setUsername] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [autologin, setAutologin] = React.useState(false);
  const [saveAccount, setSaveAccount] = React.useState(false);
  const [showCredits] = React.useState(true);
  const [musicEnabled, setMusicEnabled] = React.useState(true);
  const [musicVolume, setMusicVolume] = React.useState(100);

  // Animation state for next button
  // Note: In a custom React reconciler, animations should be handled differently
  // For now, we'll use a static state. The game engine should handle animations.
  const nextButtonAnimation = false;

  const handleLogin = () => {
    onLogin(username, password);
  };

  return (
    <>
      {/* Background layer */}
      <LoginBackground />

      {/* Main content layer */}
      <View
        backgroundColor={Colors.transparent}
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
          size={{ width: 640, height: 480 }}
          direction={ClayLayoutDirection.TopToBottom}
        >
          {/* Title Label */}
          <Label
            text="Login to Ultima Online"
            textStyle={{
              fontId: 2,
              fontSize: 24,
              textColor: Colors.white,
            }}
            floating={{
              offset: { x: 253, y: 207 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
          />

          {/* Account Name Label */}
          <Label
            text="Account Name"
            textStyle={{
              fontId: 2,
              fontSize: 16,
              textColor: Colors.white,
            }}
            floating={{
              offset: { x: 218, y: 266 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
          />

          {/* Account Name Input Background */}
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
              placeholder="Enter account name"
              textStyle={{
                fontId: 1,
                fontSize: 16,
                textColor: Colors.black,
              }}
              value={username}
              onChange={(value) => setUsername(value)}
              size={{ width: 200, height: 26 }}
              acceptInputs={true}
            />
          </Gump>

          {/* Password Label */}
          <Label
            text="Password"
            textStyle={{
              fontId: 2,
              fontSize: 16,
              textColor: Colors.white,
            }}
            floating={{
              offset: { x: 218, y: 316 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
          />

          {/* Password Input Background */}
          <Gump
            gumpId={0x0bb8}
            hue={{ x: 0, y: 0, z: 1 }}
            ninePatch={true}
            size={{ width: 210, height: 30 }}
            floating={{
              offset: { x: 218, y: 333 },
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
              placeholder="Enter password"
              password={true}
              textStyle={{
                fontId: 1,
                fontSize: 16,
                textColor: Colors.black,
              }}
              value={password}
              onChange={(value) => setPassword(value)}
              size={{ width: 200, height: 26 }}
              acceptInputs={true}
            />
          </Gump>

          {/* Quit Button */}
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

          {/* Credits Button */}
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

          {/* Next/Login Arrow Button */}
          <Button
            gumpIds={{
              normal: nextButtonAnimation ? 0x05cd : 0x05cb,
              pressed: 0x05cc,
              over: 0x05cb,
            }}
            size={{ width: 85, height: 28 }}
            hue={{ x: 0, y: 0, z: 1 }}
            floating={{
              offset: { x: 280, y: 365 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
            onClick={handleLogin}
          />

          {/* Autologin Checkbox */}
          <Checkbox
            checked={autologin}
            onChange={setAutologin}
            floating={{
              offset: { x: 153, y: 405 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
            size={{ width: 13, height: 13 }}
          />

          {/* Autologin Label */}
          <Label
            text="Autologin"
            textStyle={{
              fontId: 2,
              fontSize: 14,
              textColor: Colors.white,
            }}
            floating={{
              offset: { x: 168, y: 405 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
          />

          {/* Save Account Checkbox */}
          <Checkbox
            checked={saveAccount}
            onChange={setSaveAccount}
            floating={{
              offset: { x: 243, y: 405 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
            size={{ width: 13, height: 13 }}
          />

          {/* Save Account Label */}
          <Label
            text="Save Account"
            textStyle={{
              fontId: 2,
              fontSize: 14,
              textColor: Colors.white,
            }}
            floating={{
              offset: { x: 258, y: 405 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
          />

          {/* Music Checkbox */}
          <Checkbox
            checked={musicEnabled}
            onChange={setMusicEnabled}
            floating={{
              offset: { x: 153, y: 425 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
            size={{ width: 13, height: 13 }}
          />

          {/* Music Label */}
          <Label
            text="Music"
            textStyle={{
              fontId: 2,
              fontSize: 14,
              textColor: Colors.white,
            }}
            floating={{
              offset: { x: 168, y: 425 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
          />

          {/* Music Volume Slider */}
          <HSliderBar
            min={0}
            max={255}
            value={musicVolume}
            onChange={setMusicVolume}
            floating={{
              offset: { x: 243, y: 427 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
            size={{ width: 180, height: 10 }}
            barGumpId={0x0845}
          />

          {/* Version Labels */}
          <Label
            text="CUO v1.0.0"
            textStyle={{
              fontId: 3,
              fontSize: 12,
              textColor: Colors.gray,
            }}
            floating={{
              offset: { x: 17, y: 448 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
          />

          <Label
            text="UO v7.0.95.0"
            textStyle={{
              fontId: 3,
              fontSize: 12,
              textColor: Colors.gray,
            }}
            floating={{
              offset: { x: 17, y: 463 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
          />

          {/* Links */}
          <Label
            text="Support"
            textStyle={{
              fontId: 3,
              fontSize: 12,
              textColor: Colors.link,
            }}
            floating={{
              offset: { x: 497, y: 445 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
            onClick={() => {
              // Open support link
            }}
          />

          <Label
            text="Website"
            textStyle={{
              fontId: 3,
              fontSize: 12,
              textColor: Colors.link,
            }}
            floating={{
              offset: { x: 543, y: 445 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
            onClick={() => {
              // Open website
            }}
          />

          <Label
            text="Discord"
            textStyle={{
              fontId: 3,
              fontSize: 12,
              textColor: Colors.link,
            }}
            floating={{
              offset: { x: 591, y: 445 },
              attachTo: ClayFloatingAttachToElement.Parent,
              clipTo: ClayFloatingClipToElement.AttachedParent,
            }}
            onClick={() => {
              // Open Discord
            }}
          />
        </Gump>
      </View>
    </>
  );
};