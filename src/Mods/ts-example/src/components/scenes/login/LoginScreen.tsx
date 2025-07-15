import React, { useCallback, useMemo } from "react";
import { ChildAlign, Colors, Float, TextStyle } from "~/ui";
import {
  FloatingAttachToElement,
  FloatingClipToElement,
  LayoutAlignment,
  LayoutDirection,
  SizingType,
} from "~/host";
import { LoginContainer } from "./LoginContainer";
import { Text, Button, Gump, TextInput, Checkbox, HSliderBar } from "~/react";

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
  const [showCredits, setShowCredits] = React.useState(true);
  const [musicEnabled, setMusicEnabled] = React.useState(true);
  const [musicVolume, setMusicVolume] = React.useState(100);
  const [testText, setTestText] = React.useState("TEST");
  const [count, setCount] = React.useState(0);
  const renderCount = React.useRef(0);

  // Animation state for next button
  // Note: In a custom React reconciler, animations should be handled differently
  // For now, we'll use a static state. The game engine should handle animations.
  const nextButtonAnimation = false;

  // test memoized event handlers stick
  const handleQuit = useMemo(() => {
    const random = Math.random();
    return () => {
      console.log("handleQuit", random);
      setTestText((prev) => prev + " QUIT");
      onQuit();
    };
  }, [onQuit]);

  const handleLogin = () => {
    console.log("handleLogin");

    onLogin(username, password);
    setShowCredits(!showCredits);
    setTestText(testText + " TEST");
    setCount(count + 1);
  };

  console.log("Rendering LoginScreen");
  renderCount.current++;

  return (
    <LoginContainer>
      <Text floating={Float.offsetParent(5, 5)} style={TextStyle.title}>
        Render Count: {renderCount.current}
      </Text>
      <Text floating={Float.offsetParent(120, 120)} style={TextStyle.link}>
        <>{count}</>
        {testText}
      </Text>
      {/* Account Name Input Background */}
      <Gump
        id={0x0bb8}
        ninePatch={true}
        size={{ width: 210, height: 30 }}
        floating={Float.offsetParent(218, 283)}
        childAlignment={ChildAlign.center}
        direction={LayoutDirection.LeftToRight}
      >
        <TextInput
          placeholder="Enter account name"
          textStyle={TextStyle.input}
          value={username}
          onChange={(value) => setUsername(value)}
          size={{ width: 200, height: 30 }}
        />
      </Gump>

      {/* Password Input Background */}
      <Gump
        id={0x0bb8}
        ninePatch={true}
        size={{ width: 210, height: 30 }}
        floating={Float.offsetParent(218, 333)}
        direction={LayoutDirection.LeftToRight}
        childAlignment={ChildAlign.center}
      >
        <TextInput
          placeholder="Enter password"
          password={true}
          textStyle={TextStyle.input}
          value={password}
          onChange={(value) => setPassword(value)}
          size={{ width: 200, height: 30 }}
        />
      </Gump>

      {/* Quit Button */}
      <Button
        onClick={handleQuit}
        gumpIds={{ normal: 0x05ca, pressed: 0x05c9, over: 0x05c8 }}
        size={{ width: 66, height: 66 }}
        floating={Float.offsetParent(25, 240)}
      />

      {/* Credits Button */}
      {showCredits && (
        <Button
          onClick={onCredits}
          gumpIds={{ normal: 0x05d0, pressed: 0x05cf, over: 0x05ce }}
          size={{ width: 104, height: 36 }}
          floating={Float.offsetParent(530, 125)}
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
        floating={Float.offsetParent(280, 365)}
        onClick={handleLogin}
      />

      {/* Autologin Checkbox */}
      <Checkbox
        checked={autologin}
        onChange={setAutologin}
        floating={Float.offsetParent(153, 405)}
        size={{ width: 13, height: 13 }}
      />

      {/* Autologin Text */}
      <Text floating={Float.offsetParent(168, 405)}>Autologin</Text>

      {/* Save Account Checkbox */}
      <Checkbox
        checked={saveAccount}
        onChange={setSaveAccount}
        floating={Float.offsetParent(243, 405)}
        size={{ width: 13, height: 13 }}
      />

      {/* Save Account Text */}
      <Text floating={Float.offsetParent(258, 405)}>Save Account</Text>

      {/* Music Checkbox */}
      <Checkbox
        checked={musicEnabled}
        onChange={setMusicEnabled}
        floating={Float.offsetParent(153, 425)}
        size={{ width: 13, height: 13 }}
      />

      {/* Music Text */}
      <Text floating={Float.offsetParent(168, 425)}>Music</Text>

      {/* Music Volume Slider */}
      <HSliderBar
        min={0}
        max={255}
        value={musicVolume}
        onChange={setMusicVolume}
        floating={Float.offsetParent(243, 427)}
        size={{ width: 180, height: 10 }}
        barGumpId={0x0845}
      />

      {/* Version Texts */}
      <Text floating={Float.offsetParent(17, 448)}>CUO v1.0.0</Text>

      <Text floating={Float.offsetParent(17, 463)}>UO v7.0.95.0</Text>

      {/* Links */}
      <Text
        style={TextStyle.link}
        floating={Float.offsetParent(497, 445)}
        onClick={() => {
          // Open support link
        }}
      >
        Support
      </Text>

      <Text
        style={TextStyle.link}
        floating={Float.offsetParent(543, 445)}
        onClick={() => {
          // Open website
        }}
      >
        Website
      </Text>

      <Text
        style={TextStyle.link}
        floating={Float.offsetParent(591, 445)}
        onClick={() => {
          // Open Discord
        }}
      >
        Discord
      </Text>
    </LoginContainer>
  );
};
